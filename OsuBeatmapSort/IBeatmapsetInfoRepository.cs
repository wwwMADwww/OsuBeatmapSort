using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace OsuBeatmapSort
{
    public interface IBeatmapsetInfoRepository<TBeatmapsetDirectory, TBeatmapsetInfo>
    {
        BufferBlock<TBeatmapsetInfo> GetBeatmapsetInfo(BufferBlock<string> beatmapsetDirsBuffer);
    }


}
