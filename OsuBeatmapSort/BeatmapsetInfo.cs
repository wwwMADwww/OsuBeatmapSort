using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuElements.Beatmaps;

namespace OsuBeatmapSort
{
    public class BeatmapsetInfo
    {

        public int Id { get; set; }

        public string DirectoryName { get; set; }

        public IEnumerable<Beatmap> Beatmaps { get; set; }

        public BeatmapsetStatus Status { get; set; }

    }

    public enum BeatmapsetStatus { None, NotPlayed, Played, Error }

}
