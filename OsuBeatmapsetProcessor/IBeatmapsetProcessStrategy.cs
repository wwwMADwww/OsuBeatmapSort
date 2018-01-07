using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    public interface IBeatmapsetProcessStrategy<TBeatmapsetInfo>
    {
        void Process(IEnumerable<TBeatmapsetInfo> beatmapsetInfoList);
    }
}
