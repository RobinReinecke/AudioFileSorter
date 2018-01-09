using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NDesk.Options;

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

        private static void Main(string[] args)
        {
            var source_dir = "";
            var destination_dir = "";
            var output_moves = false;
            var show_help = false;
            var custom_formats = new List<string>();

            //ndesk parameters
            var parameters = new OptionSet()
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
                },
                {
                    "h|help",  "show this message and exit.",
                    v => show_help = v != null
                }
            };

            //parse the options
            try
            {
                parameters.Parse(args);

                if (show_help)
                {
                    ShowHelp(parameters);
                    return;
                }
                if (source_dir == null)
                    throw new OptionException("Missing parameter", source_dir);
                if (destination_dir == null)
                    throw new OptionException("Missing parameter", destination_dir);
                if (!Directory.Exists(destination_dir))
                    throw new OptionException("Directory does not exist.", destination_dir);
                if (!Directory.Exists(source_dir))
                    throw new OptionException("Directory does not exist.", source_dir);
            }
            catch (OptionException ex)
            {
                Console.Write(ex.Message + "\n" + "Try 'AudioFileSorter.exe --help' for more information. ");
                return;
            }

            //Add custom media formats
            media_formats.AddRange(custom_formats);

            CreateDirStructur(destination_dir);
            ProcessDirectory(source_dir, destination_dir, output_moves);

            Console.WriteLine("\n\nFinished");
        }

        /// <summary>
        /// Create the directory structur for sorting the files to.
        /// </summary>
        private static void CreateDirStructur(string destination_Dir)
        {
            Directory.CreateDirectory(destination_Dir + "\\0-9");
            Directory.CreateDirectory(destination_Dir + "\\_unsortable");

            foreach (var c in alphabet)
                Directory.CreateDirectory(destination_Dir + "\\" + c);
        }

        /// <summary>
        /// Main method to sort all the files.
        /// </summary>
        private static void ProcessDirectory(string source_Dir, string destination_Dir, bool output_Moves)
        {
            foreach (var file in GetFiles(source_Dir, media_formats, SearchOption.AllDirectories))
            {
                //get media information
                var tagFile = TagLib.File.Create(file);
                var performers = tagFile.Tag.Performers;
                var album = tagFile.Tag.Album;
                var title = tagFile.Tag.Title;

                if (performers == null || album == null || title == null)
                {
                    Console.WriteLine("Can't read enough information to sort " + file);
                    continue;
                }

                var primaryPerformer = performers[0];

                //Case of multiple performers. Don't know if its possible
                if (performers.Length > 1)
                {
                    var output = "Multiple performers found. Which is the primary performer? ";
                    for (var i = 1; i <= performers.Length; i++)
                        output += i + ". " + performers[i - 1] + " ";
                    Console.WriteLine(output);
                    var selectedIndex = -1;

                    while (selectedIndex > performers.Length || selectedIndex < 1)
                    {
                        Console.Write("Index: ");
                        int.TryParse(Console.ReadLine(), out selectedIndex);
                    }

                    primaryPerformer = performers[selectedIndex - 1];
                }

                var destination = destination_Dir;

                if (Regex.IsMatch(primaryPerformer[0].ToString(), "[a-zöäü]", RegexOptions.IgnoreCase))
                {
                    destination += "\\" + primaryPerformer[0].ToString().ToUpper() + "\\" + primaryPerformer;
                    Directory.CreateDirectory(destination);
                    destination += "\\" + album;
                    Directory.CreateDirectory(destination);
                    destination += "\\" + Path.GetFileName(file);
                }  
                else if (char.IsNumber(primaryPerformer[0]))
                { 
                    destination += "\\0-9\\" + primaryPerformer;
                    Directory.CreateDirectory(destination);
                    destination += "\\" + album;
                    Directory.CreateDirectory(destination);
                    destination += "\\" + Path.GetFileName(file);
                }
                else
                    destination += "\\_unsortable\\" + Path.GetFileName(file);

                try
                {
                    File.Move(file, destination);
                    if (output_Moves)
                        Console.WriteLine("Moved " + file + " to " + destination);
                }
                catch (Exception)
                {
                    Console.WriteLine("Can't move file " + file + " to " + destination);
                }
            }
        }

        /// <summary>
        /// Output the help.
        /// </summary>
        private static void ShowHelp(OptionSet parameters)
        {
            Console.WriteLine("Usage: AudioFileSorter.exe [OPTIONS]\nMove all audio files in a given directory in a sorted way to the destination directory.\nOptions:");
            parameters.WriteOptionDescriptions(Console.Out);
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
