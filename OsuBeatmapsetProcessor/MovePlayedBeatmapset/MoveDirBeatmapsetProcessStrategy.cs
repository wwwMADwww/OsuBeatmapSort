using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{

    public class MoveDirBeatmapsetProcessStrategy : IBeatmapsetProcessStrategy<BeatmapsetInfo>
    {
        private readonly string _mapsetPlayedDir;
        private readonly string _mapsetNotPlayedDir;
        private readonly Logger _logger;

        public MoveDirBeatmapsetProcessStrategy(
            string mapsetPlayedDir,
            string mapsetNotPlayedDir
            )
        {
            _mapsetPlayedDir    = Path.GetFullPath(mapsetPlayedDir);
            _mapsetNotPlayedDir = Path.GetFullPath(mapsetNotPlayedDir);
            
            _logger = LogManager.GetLogger("logger");

            if (!Directory.Exists(_mapsetPlayedDir))    Directory.CreateDirectory(_mapsetPlayedDir);
            if (!Directory.Exists(_mapsetNotPlayedDir)) Directory.CreateDirectory(_mapsetNotPlayedDir);

            _logger.Info($"Dir for played beatmapsets is '{_mapsetPlayedDir}'.");
            _logger.Info($"Dir for not played beatmapsets is '{_mapsetNotPlayedDir}'.");
        }

        
        public async Task Process(BufferBlock<BeatmapsetInfo> beatmapsetInfoList)
        {

            while(true)
            {
                

                if (!await beatmapsetInfoList.OutputAvailableAsync())
                {
                    _logger.Trace($"Beatmapset info channel closed.");
                    break;
                }

                if (!beatmapsetInfoList.TryReceive(x => true, out BeatmapsetInfo beatmapsetInfo))
                    continue;

                string destinationDir = null;

                try
                {
                    if (beatmapsetInfo.IsPlayed == null)
                    {
                        _logger.Warn($"Beatmapset '{beatmapsetInfo.Directory}' state is unknown. Leaving as is.");
                    }
                    else
                    { 
                        if (beatmapsetInfo.IsPlayed == true)
                        {
                            destinationDir = Path.Combine(_mapsetPlayedDir, Path.GetFileName(beatmapsetInfo.Directory));
                        }
                        else if (beatmapsetInfo.IsPlayed == false)
                        {
                            destinationDir = Path.Combine(_mapsetNotPlayedDir, Path.GetFileName(beatmapsetInfo.Directory));
                        }

                        _logger.Info($"Moving beatmapset dir '{beatmapsetInfo.Directory}' to '{destinationDir}'.");
                        Directory.Move(beatmapsetInfo.Directory, destinationDir);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"Error occured during moving beatmapset dir '{beatmapsetInfo.Directory}' to '{destinationDir}':"+ 
                        $"{Environment.NewLine}{e.ToString()}");
                }

            } // /while


        }
    }
}
