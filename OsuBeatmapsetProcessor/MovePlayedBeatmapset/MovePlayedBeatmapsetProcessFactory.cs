using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    public class MovePlayedBeatmapsetProcessFactory : IBeatmapsetProcessFactory
    {
        private readonly IFactoryParams _factoryParams;

        public MovePlayedBeatmapsetProcessFactory(
            IFactoryParams factoryParams
            )
        {
            _factoryParams = factoryParams;
        }

        public IBeatmapsetProcess Create()
        {

            // настройки логгера находятся в app.config
            var logger = LogManager.GetLogger("logger");

            var directoryRepo = new BeatmapsetDirectoryRepository(_factoryParams.SongsDir);

            var infoRepo = new PlayedBeatmapsetInfoRepository(
                _factoryParams.UserID,
                _factoryParams.ApiKey,
                _factoryParams.ThreadCount,
                _factoryParams.OsuDbFilename,
                logger
            );

            var processStrategy = new MoveDirBeatmapsetProcessStrategy(
                _factoryParams.MapsetPlayedDir, 
                _factoryParams.MapsetNotPlayedDir, 
                logger
            );
            

            var processor = new MovePlayedBeatmapsetProcess(
                directoryRepo,
                infoRepo,
                processStrategy
            );
            
            return processor;
        }

        
        public interface IFactoryParams
        {
            String ApiKey { get; }
            
            int UserID { get; }
            
            int ThreadCount { get; }
            
            String OsuDbFilename { get; }
            
            String SongsDir { get; }
            
            String MapsetPlayedDir { get; }
            
            String MapsetNotPlayedDir { get; }

        }
    }
    
}
