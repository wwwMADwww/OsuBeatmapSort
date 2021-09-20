using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace OsuBeatmapSort
{
    public class AppOptionsParser
    {
        const string nullDir = "?";

        public static bool Parse(out AppOptions options)
        {
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            AppOptions optbuf = null;
            var result = CommandLine.Parser.Default.ParseArguments<AppOptions>(args);

            var res = result.MapResult
            ( 
                (AppOptions opt) =>
                {
                    optbuf = opt;
                    return true;
                },
                errors => false
            );

            if (res && optbuf != null)
            { 

                if (optbuf.DirError == nullDir)
                    optbuf.DirError = null;

                if (optbuf.DirPlayed == nullDir)
                    optbuf.DirPlayed = null;

                if (optbuf.DirNotPlayed == nullDir)
                    optbuf.DirNotPlayed = null;

            }

            options = optbuf;

            return res;
        }


    }



    public class AppOptions
    {
        [Option("apiKey", Required = true,
          HelpText = "osu!api access key.")]
        public string ApiKey { get; set; }

        [Option("apiRpm", Default = 0,  Required = false,
          HelpText = "Maximum requests to osu!api per minute. Zero means no limit.")]
        public int ApiRpm { get; set; }

        [Option("apiThreads", Default = 8, Required = false,
          HelpText = "Number of threads for requesting info from osu!api. Useless if apiRpm is set.")]
        public int ApiThreads { get; set; }



        [Option("playerId", Required = true,
          HelpText = "Player ID (ID, not nickname)")]
        public int PlayerId { get; set; }



        [Option("dbFilename", Default = "osu!.db", Required = false,
          HelpText = "Path to osu!.db file.")]
        public string OsuDbFilename { get; set; }

        [Option("dirGame", Required = true,
          HelpText = "Path to the game directory. Will be used as base path for any relative path.")]
        public string dirGame { get; set; }

        [Option("dirSongs", Default = "Songs", Required = false,
          HelpText = "Path to beatmapsets directory.")]
        public string DirSongs { get; set; }


        [Option("dirPlayed", Default = "SongsPlayed", Required = false,
          HelpText = "Dir for played beatmapsets. '?' means that mapsets will not be moved.")]
        public string DirPlayed { get; set; }

        [Option("dirNotPlayed", Default = "SongsNotPlayed", Required = false,
          HelpText = "Dir for not played beatmapsets. '?' means that mapsets will not be moved.")]
        public string DirNotPlayed { get; set; }

        [Option("dirError", Default = "SongsError", Required = false,
          HelpText = "Dir for beatmapsets during processing which error is occurred. '?' means that mapsets will not be moved.")]
        public string DirError { get; set; }

    }

}
