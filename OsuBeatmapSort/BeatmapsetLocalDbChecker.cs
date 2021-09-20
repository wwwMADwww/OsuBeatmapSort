using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using osuElements.Beatmaps;
using osuElements.Db;

namespace OsuBeatmapSort
{
    public class BeatmapsetLocalDbChecker
    {
        private readonly Logger _logger;
        private readonly string _logPrefix = "[Local]";

        private readonly string _osuDbPath;
        private readonly string _songsDir;
        private Dictionary<string, DbBeatmap[]> _dbMapsets;

        public BeatmapsetLocalDbChecker(string osuDbPath, string songsDir)
        {
            _logger = LogManager.GetLogger("logger");
            _osuDbPath = osuDbPath;
            _songsDir = songsDir;
        }



        public bool Init()
        {
            try
            {
                _logger.Info($"{_logPrefix} Reading local osu database");

                if (!File.Exists(_osuDbPath))
                {
                    _logger.Error($"{_logPrefix} Osu database file not found '{_osuDbPath}'");
                    return false;
                }

                var osudb = new OsuDb
                {
                    FullPath = _osuDbPath
                };
                osudb.ReadFile();

                _dbMapsets = osudb.Beatmaps
                    .GroupBy(b => b.Directory)
                    .ToDictionary(g => g.Key, g => g.ToArray());

            }
            catch (Exception e)
            {
                _logger.Error($"{_logPrefix} Error occurred during BeatmapsetLocalDbChecker initialization: {Environment.NewLine}{e.ToString()}");
                return false;
            }

            return true;
        }



        public BeatmapsetInfo CheckAndPopulate(BeatmapsetInfo mapsetInfo)
        {

            string dirFullPath = null; 

            try
            {
                dirFullPath = Path.Combine(_songsDir, mapsetInfo.DirectoryName);

                if (_dbMapsets.TryGetValue(mapsetInfo.DirectoryName, out var dbbeatmaps))
                {
                    mapsetInfo.Beatmaps = dbbeatmaps;
                    mapsetInfo.Status = dbbeatmaps.Any(b => !b.Unplayed) 
                        ? BeatmapsetStatus.Played 
                        : BeatmapsetStatus.NotPlayed;
                    mapsetInfo.Id = dbbeatmaps
                        .Where(b => b.BeatmapSetId > 0)
                        .Select(b => b.BeatmapSetId)
                        .DefaultIfEmpty(0)
                        .FirstOrDefault();
                }
                else
                {
                    var beatmaps = Directory.GetFiles(dirFullPath, "*.osu")
                        .Select(f => new Beatmap(f))
                        .ToArray();

                    if (!beatmaps.Any())
                    {
                        mapsetInfo.Status = BeatmapsetStatus.Error;
                        _logger.Warn($"{_logPrefix} '{mapsetInfo.DirectoryName}' Beatmapset dir does not contain .osu files.");
                    }
                    else
                    {
                        mapsetInfo.Beatmaps = beatmaps;
                        mapsetInfo.Status = BeatmapsetStatus.NotPlayed;
                        mapsetInfo.Id = beatmaps
                            .Where(b => b.BeatmapSetId > 0)
                            .Select(b => b.BeatmapSetId)
                            .DefaultIfEmpty(0)
                            .FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                mapsetInfo.Status = BeatmapsetStatus.Error;

                _logger.Error($"{_logPrefix} '{mapsetInfo.DirectoryName}' Error occurred during checking beatmapset: " +
                    $"{Environment.NewLine}{e.ToString()}");
            }

            return mapsetInfo;

        }

    }
}
