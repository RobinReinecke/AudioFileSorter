using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Colorful;
using NDesk.Options;
using static System.Int32;
using Console = Colorful.Console;

namespace AudioFileSorter
{
    internal class Program
    {

        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZÖÄÜ";

        //https://en.wikipedia.org/wiki/Audio_file_format
        private static List<string> media_formats = new List<string>()
        {
            "*.3gp",
            "*.aa",
            "*.aac",
            "*.aax",
            "*.act",
            "*.aiff",
            "*.amr",
            "*.ape",
            "*.au",
            "*.awb",
            "*.dct",
            "*.dss",
            "*.dvf",
            "*.flac",
            "*.gsm",
            "*.iklax",
            "*.ivs",
            "*.m4a",
            "*.m4b",
            "*.m4p",
            "*.mmf",
            "*.mp3",
            "*.mpc",
            "*.msv",
            "*.ogg",
            "*.oga",
            "*.mogg",
            "*.opus",
            "*.ra",
            "*.rm",
            "*.raw",
            "*.sln",
            "*.tta",
            "*.vox",
            "*.wav",
            "*.wma",
            "*.wv",
            "*.webm",
            "*.8svx",
        };

        /// <summary>
        /// ndesk parameters
        /// </summary>
        private static readonly OptionSet parameters = new OptionSet()
        {
            {
                "s|source=", "the {DIRECTORY} with the files to sort.",
                v => source_dir = v
            },
            {
                "d|destination=", "the destination parent {DIRECTORY}.",
                v => destination_dir = v
            },
            {
                "o|output", "should the actions be outputed.",
                v => output_moves = v != null
            },
            {
                "f|format=", "custom {FORMAT} to sort.",
                v => custom_formats.Add(v)
            }
        };

        /// <summary>
        /// Source dir of audio files.
        /// </summary>
        private static string source_dir = "";

        /// <summary>
        /// Destination dir.
        /// </summary>
        private static string destination_dir = "";

        /// <summary>
        /// Option to output the moves
        /// </summary>
        private static bool output_moves = false;

        /// <summary>
        /// Custom formats of audio files.
        /// </summary>
        private static List<string> custom_formats = new List<string>();

        /// <summary>
        /// Main
        /// </summary>
        private static void Main()
        {
            Console.WriteAscii("AFS BY ROBI", Color.Gold);  

            var correct_options = false;

            //parse the options
            while (!correct_options)
            {
                try
                {
                    var options = RequireInput();
                    parameters.Parse(options);

                    if (source_dir == null)
                        throw new OptionException("\nMissing parameter", source_dir);
                    if (destination_dir == null)
                        throw new OptionException("\nMissing parameter", destination_dir);
                    if (!Directory.Exists(destination_dir))
                        throw new OptionException("\nDirectory does not exist.", destination_dir);
                    if (!Directory.Exists(source_dir))
                        throw new OptionException("\nDirectory does not exist.", source_dir);

                    //else the options are correct
                    correct_options = true;
                }
                catch (OptionException ex)
                {
                    Console.Write(ex.Message + "\n", Color.Red);
                }
            }

            //Add custom media formats
            media_formats.AddRange(custom_formats);

            CreateDirStructur();

            ProcessDirectory();

            Console.WriteLine("\n\nFinished");
        }

        /// <summary>
        /// Start of program to require settings.
        /// </summary>
        private static string[] RequireInput()
        {
            Console.WriteLine(
                "\nDescription: Move all audio files in a given directory in a sorted way to the destination directory.\n");
            ShowHelp();
            Console.Write("Options: ");
            return Console.ReadLine().Split(' ');
        }

        /// <summary>
        /// Output the help.
        /// </summary>
        private static void ShowHelp()
        {
            parameters.WriteOptionDescriptions(Console.Out);
        }


        /// <summary>
        /// Create the directory structur for sorting the files to.
        /// </summary>
        private static void CreateDirStructur()
        {
            Console.WriteLine("\nCreating Directories.", true);
            Directory.CreateDirectory(destination_dir + "\\0-9");
            Directory.CreateDirectory(destination_dir + "\\_unsortable");

            foreach (var c in alphabet)
                Directory.CreateDirectory(destination_dir + "\\" + c);
        }

        /// <summary>
        /// Main method to sort all the files.
        /// </summary>
        private static void ProcessDirectory()
        {
            var files = GetFiles(source_dir, media_formats, SearchOption.AllDirectories);
            var enumerable = files as string[] ?? files.ToArray();
            Console.Write("Found " + enumerable.Count() + " files.");

            foreach (var file in enumerable)
            {
                //get media information
                var tagFile = TagLib.File.Create(file);
                var albumArtist = tagFile.Tag.FirstAlbumArtist;
                var album = tagFile.Tag.Album;
                var title = tagFile.Tag.Title;

                if (albumArtist == null || album == null || title == null)
                {
                    Console.Write("Can't read enough information to sort ");
                    Console.Write(file + "\n", Color.Tomato);
                    continue;
                }

                var destination = destination_dir;

                //check if first letter of album matches the alphabet
                if (Regex.IsMatch(albumArtist[0].ToString(), "[a-zöäü]", RegexOptions.IgnoreCase))
                {
                    destination += "\\" + albumArtist[0].ToString().ToUpper() + "\\" + FixInvalidDirNameChars(albumArtist);

                    destination = CreateDirOrSelectExisting(destination);

                    destination += "\\" + FixInvalidDirNameChars(album);

                    destination = CreateDirOrSelectExisting(destination);

                    destination += "\\" + Path.GetFileName(file);
                }
                //else chef it first letter is a number
                else if (char.IsNumber(albumArtist[0]))
                {
                    destination += "\\0 - 9\\" + FixInvalidDirNameChars(albumArtist);

                    destination = CreateDirOrSelectExisting(destination);

                    destination += "\\" + FixInvalidDirNameChars(album);

                    destination = CreateDirOrSelectExisting(destination);

                    destination += "\\" + Path.GetFileName(file);
                }
                //else put it to unsortable
                else
                    destination += "\\_unsortable\\" + Path.GetFileName(file);

                try
                {
                    File.Move(file, destination);
                    if (output_moves) { 
                        Console.Write("Moved ");
                        Console.Write(Path.GetFileName(destination), Color.Aqua);
                        Console.Write(" with Title: ");
                        Console.Write(title, Color.Aqua);
                        Console.Write(" and artist: ");
                        Console.Write(albumArtist, Color.Aqua);
                        Console.Write(" and Album: ");
                        Console.Write(album, Color.Aqua);
                        Console.Write(" to ");
                        Console.Write(destination, Color.Tomato);

                    }
                    WriteToLog("Moved " + file + " to " + destination + "\n");


                }
                catch (Exception ex)
                {
                    var message = "Can't move file " + file + " to " + destination + " because " + ex.Message + "\n";
                    WriteToLog(message);
                    Console.WriteLine(message, Color.Red);
                }
            }
        }

        /// <summary>
        /// Checks for existing dirs with same name or similar name.
        /// Using Damerau–Levenshtein distance from http://mihkeltt.blogspot.de/2009/04/dameraulevenshtein-distance.html.
        /// </summary>
        private static string CreateDirOrSelectExisting(string destination)
        {
            //if the directory exists, nothing to create or check
            if (Directory.Exists(destination))
                return destination;

            //get parent dir
            var parent_info = new DirectoryInfo(destination.Substring(0, destination.LastIndexOf('\\')));

            var destination_name = destination.Substring(destination.LastIndexOf('\\'), destination.Length - destination.LastIndexOf('\\'));

            var possbile_dirs = (from child_dir in Directory.GetDirectories(parent_info.FullName)
                                 let child_info = new DirectoryInfo(child_dir)
                                 let distance = destination_name.DamerauLevenshteinDistanceTo(child_info.Name)
                                 where distance <= 3
                                 select child_dir).
                                 ToList();

            //if no similar dirs exists, create the destination dir
            if (possbile_dirs.Count == 0)
            {
                Directory.CreateDirectory(destination);
                return destination;
            }
            else
            {
                Console.WriteLine("Similar folders found to " + destination_name + ". " +
                              "Pleaes select one by typing in the number or type 0 for creating the folder." );

                for (var i = 0; i < possbile_dirs.Count; i++)
                {
                    var possbile_Dir = possbile_dirs[i];
                    Console.Write(i + 1 + ". ");
                    Console.WriteLine(possbile_Dir, Color.Tomato);
                }

                Console.Write("Selection: ");
                var selection = -1;
                while (selection < 0 || selection > possbile_dirs.Count)
                {
                    TryParse(Console.ReadLine(), out selection);
                }

                if (selection == 0)
                {
                    Directory.CreateDirectory(destination);
                    return destination;
                }
                else
                {
                    Directory.CreateDirectory(possbile_dirs[selection - 1]);
                    return possbile_dirs[selection - 1];
                }
            }
           
        }

        /// <summary>
        /// Replace unallowed chars with _ in dir names.
        /// </summary>
        private static string FixInvalidDirNameChars(string importstring)
        {
            foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                importstring = importstring.Replace(invalidFileNameChar, '_');
            }
            return importstring;
        }

        /// <summary>
        /// Write to logfile.
        /// </summary>
        private static void WriteToLog(string message)
        {
            try
            {
                using (var l_Writer = new StreamWriter("log.txt", true))
                    l_Writer.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + message);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Takes multiple search patterns.
        /// Thanks to Mikael Svenson https://stackoverflow.com/questions/3754118/how-to-filter-directory-enumeratefiles-with-multiple-criteria
        /// </summary>
        public static IEnumerable<string> GetFiles(string path, List<string> searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel()
                .SelectMany(searchPattern =>
                    Directory.EnumerateFiles(path, searchPattern, searchOption));
        }
    }
}
