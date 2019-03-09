using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    public class BeatmapsetDirectoryRepository : IBeatmapsetDirectoryRepository<string>
    {
        private readonly string _songsDir;
        private readonly int _tasksCount;
        private readonly Logger _logger;

        public BeatmapsetDirectoryRepository(
            string songsDir,
            int tasksCount
            )
        {
            _songsDir = songsDir;
            _tasksCount = tasksCount;
            
            _logger = LogManager.GetLogger("logger");
        }

        public BufferBlock<string> GetDirectories()
        {
            var dirChan = new BufferBlock<string>(new DataflowBlockOptions(){BoundedCapacity = _tasksCount * 10});

            Task.Factory.StartNew(async () => {
                foreach (var d in Directory.EnumerateDirectories(_songsDir)) {
                    await dirChan.SendAsync(d);
                };
                _logger.Info("Dir info channel closing.");
                dirChan.Complete();
            });

            return dirChan;

        }
    }
}
