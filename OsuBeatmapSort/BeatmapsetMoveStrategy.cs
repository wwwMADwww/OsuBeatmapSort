using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace OsuBeatmapSort
{

    public class BeatmapsetMoveStrategy
    {
        private readonly Logger _logger;
        private readonly string _logPrefix = "[Move ]";
        private readonly string _songsDir;
        Dictionary<BeatmapsetStatus, string> _targetDirs = new Dictionary<BeatmapsetStatus, string>();

        public BeatmapsetMoveStrategy(
            Dictionary<BeatmapsetStatus, string> targetDirs,
            string songsDir
            )
        {
            _logger = LogManager.GetLogger("logger");

            _targetDirs = targetDirs
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            _songsDir = songsDir;
        }



        public bool Init()
        {

            _logger.Info($"{_logPrefix} BeatmapsetMoveStrategy init.");

            try
            {
                foreach (var (status, dir) in _targetDirs)
                {
                    var fullpath = Path.GetFullPath(dir);

                    if (!Directory.Exists(fullpath))
                        Directory.CreateDirectory(fullpath);

                    _targetDirs[status] = fullpath;

                    _logger.Info($"{_logPrefix} Dir for status {status} is '{dir}', full path is '{fullpath}'.");
                }

            }
            catch (Exception e)
            {
                _logger.Error($"{_logPrefix} Error occurred during BeatmapsetMoveStrategy initialization: {Environment.NewLine}{e.ToString()}");
                return false;
            }

            return true;
        }

        
        public void Move(BeatmapsetInfo beatmapsetInfo)
        {
            string destinationDir = null;
            try
            {
                if (_targetDirs.TryGetValue(beatmapsetInfo.Status, out var targetDirPath))
                {
                    var sourceDir = Path.Combine(_songsDir, beatmapsetInfo.DirectoryName);
                    destinationDir = Path.Combine(targetDirPath, beatmapsetInfo.DirectoryName);

                    _logger.Info($"{_logPrefix} '{beatmapsetInfo.DirectoryName}' Beatmapset status is {beatmapsetInfo.Status}, moving to '{targetDirPath}'.");
                    Directory.Move(sourceDir, destinationDir);
                }
                else 
                {
                    _logger.Info($"{_logPrefix} '{beatmapsetInfo.DirectoryName}' Beatmapset status is {beatmapsetInfo.Status}, leaving as is.");
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{_logPrefix} '{beatmapsetInfo.DirectoryName}' Error occurred during moving beatmapset dir to '{destinationDir}':"+ 
                    $"{Environment.NewLine}{e.ToString()}");
            }
        }
    }
}
