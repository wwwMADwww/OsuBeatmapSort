using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    public class MovePlayedBeatmapsetProcessFactory : IBeatmapsetProcessFactory
    {
        private readonly MovePlayedBeatmapsetProcessOptions _factoryParams;

        public MovePlayedBeatmapsetProcessFactory(
            MovePlayedBeatmapsetProcessOptions factoryParams
            )
        {
            _factoryParams = factoryParams;
        }

        public IBeatmapsetProcess Create()
        {

            var directoryRepo = new BeatmapsetDirectoryRepository(
                _factoryParams.SongsDir,
                _factoryParams.TasksCount
            );

            var infoRepo = new PlayedBeatmapsetInfoRepository(
                _factoryParams.UserID,
                _factoryParams.ApiKey,
                _factoryParams.TasksCount,
                _factoryParams.OsuDbFilename
            );

            var processStrategy = new MoveDirBeatmapsetProcessStrategy(
                _factoryParams.MapsetPlayedDir, 
                _factoryParams.MapsetNotPlayedDir
            );
            

            var processor = new MovePlayedBeatmapsetProcess(
                directoryRepo,
                infoRepo,
                processStrategy
            );
            
            return processor;
        }

    }

    

    #region MovePlayedBeatmapset options

    [Verb("MovePlayedBeatmapset")]
    public class MovePlayedBeatmapsetProcessOptions: IOptions
    {
        [Option("apikey", Required = true,
          HelpText = "osu!api access key.")]
        public String ApiKey { get; set; }

        [Option("playerid", Required = true,
          HelpText = "Player ID (not nickname)")]
        public int UserID { get; set; }

        [Option("TasksCount", Default = 20, Required = false,
          HelpText = "Processing tasks count.")]
        public int TasksCount { get; set; }

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
