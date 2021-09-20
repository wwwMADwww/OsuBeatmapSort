using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace OsuBeatmapSort
{
    public class BeatmapsetProvider
    {
        private readonly Logger _logger;
        private readonly string _logPrefix = "[Songs]";

        private readonly string _songsDir;

        public BeatmapsetProvider(string songsDir)
        {
            _logger = LogManager.GetLogger("logger");
            _songsDir = songsDir;
        }


        public bool Init()
        {
            _logger.Info($"{_logPrefix} BeatmapsetProvider init.");
            try
            {
                var dirFullPath = Path.GetFullPath(_songsDir);
                if (!Directory.Exists(dirFullPath))
                {
                    _logger.Error($"{_logPrefix} Songs directory does not exists '{_songsDir}', full path '{dirFullPath}'");
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{_logPrefix} Error occurred during BeatmapsetProvider initialization: {Environment.NewLine}{e.ToString()}");
                return false;
            }

            return true;
        }



        public IEnumerable<BeatmapsetInfo> GetMapsets()
        {
            var dirs = Directory.GetDirectories(_songsDir);

            if (!dirs?.Any() ?? true)
            {
                _logger.Info($"{_logPrefix} Songs dir '{_songsDir}' does not contain directories.");
                yield break;
            }

            _logger.Info($"{_logPrefix} Listing beatmapsets from Songs dir '{_songsDir}'");
            foreach (var dir in dirs)
            {
                BeatmapsetInfo res;

                try
                {
                    res = new BeatmapsetInfo()
                    {
                        DirectoryName = Path.GetFileName(dir),
                        Beatmaps = null
                    };
                }
                catch (Exception e)
                {
                    _logger.Error($"{_logPrefix} Error occurred during reading beatmapset dir '{dir}': " +
                        $"{Environment.NewLine}{e.ToString()}");

                    res = new BeatmapsetInfo()
                    {
                        DirectoryName = Path.GetFileName(dir),
                        Status = BeatmapsetStatus.Error,
                        Beatmaps = null
                    };
                }

                yield return res;

            } // /foreach songsDir

            yield break;

        }

    }
}
