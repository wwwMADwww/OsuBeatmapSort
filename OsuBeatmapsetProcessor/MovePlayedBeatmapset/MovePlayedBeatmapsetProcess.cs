using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    public class MovePlayedBeatmapsetProcess : BeatmapsetProcessBase<string, BeatmapsetInfo>
    {
        public MovePlayedBeatmapsetProcess(
            IBeatmapsetDirectoryRepository<string> beatmapsetDirectoriesListStrategy, 
            IBeatmapsetInfoRepository<string, BeatmapsetInfo> beatmapsetInfoRepository, 
            IBeatmapsetProcessStrategy<BeatmapsetInfo> beatmapsetProcessStrategy)
            : base(beatmapsetDirectoriesListStrategy, beatmapsetInfoRepository, beatmapsetProcessStrategy)
        {
        }
    }

    public class BeatmapsetInfo
    {
        public string Directory { get; set; }
        public bool? IsPlayed { get; set; }
    }

}