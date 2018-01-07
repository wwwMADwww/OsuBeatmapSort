using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    public class BeatmapsetDirectoryRepository : IBeatmapsetDirectoryRepository<string>
    {
        private readonly string _songsDir;

        public BeatmapsetDirectoryRepository(
            string songsDir
            )
        {
            _songsDir = songsDir;
        }

        public IEnumerable<string> GetDirectories()
        {
            return Directory.EnumerateDirectories(_songsDir)
                .Select(s => Path.Combine(_songsDir, s))
                .ToArray();
        }
    }
}
