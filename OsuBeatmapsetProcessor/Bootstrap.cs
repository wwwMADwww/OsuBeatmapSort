using CommandLine;
using OsuBeatmapsetProcessor.MovePlayedBeatmapset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    public class Bootstrap
    {

        public IBeatmapsetProcess GetBeatmapsetProcess()
        {

            IOptions options = null;
            if (!ProcessParams(out options) || options == null)
                return null;

            IBeatmapsetProcessFactory factory = null;

            if (options is MovePlayedBeatmapsetProcessFactory.IFactoryParams)
                factory = new MovePlayedBeatmapsetProcessFactory((MovePlayedBeatmapsetProcessFactory.IFactoryParams) options);

            // else if (options is )
            //   factory = 

            var process = factory.Create();

            return process;
        }
        
        bool ProcessParams(out IOptions options)
        {
            String[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            IOptions optbuf = null;
            var result = CommandLine.Parser.Default.ParseArguments<Options, MovePlayedBeatmapsetProcessOptions>(args);

            var res = result.MapResult
            ( 
                (Options opt) => true,
                (MovePlayedBeatmapsetProcessOptions opt) =>
                {
                    optbuf = opt;
                    return true;
                },
                errors => false
            );
            options = optbuf;
            return res;
        }


    }

    interface IOptions { }


    [Verb("Stub")]
    class Options: IOptions { }

    #region MovePlayedBeatmapset options

    [Verb("MovePlayedBeatmapset")]
    class MovePlayedBeatmapsetProcessOptions: IOptions, MovePlayedBeatmapsetProcessFactory.IFactoryParams
    {
        [Option("apikey", Required = true,
          HelpText = "osu!api access key.")]
        public String ApiKey { get; set; }

        [Option("playerid", Required = true,
          HelpText = "Player ID")]
        public int UserID { get; set; }

        [Option("ThreadCount", Default = 16, Required = false,
          HelpText = "Number of threads for parallel processing.")]
        public int ThreadCount { get; set; }

        [Option("OsuDbFilename", Default = "osu!.db", Required = false,
          HelpText = "Path to osu!.db file.")]
        public String OsuDbFilename { get; set; }

        [Option("SongsDir", Default = "Songs", Required = false,
          HelpText = "Path to beatmapsets directory.")]
        public String SongsDir { get; set; }


        [Option("mapsetPlayedDir", Default = "SongsPlayed", Required = false,
          HelpText = "Dir for played beatmapsets.")]
        public String MapsetPlayedDir { get; set; }

        [Option("mapsetNotPlayedDir", Default = "SongsNotPlayed", Required = false,
          HelpText = "Dir for not played beatmapsets.")]
        public String MapsetNotPlayedDir { get; set; }

    }

    #endregion

}
