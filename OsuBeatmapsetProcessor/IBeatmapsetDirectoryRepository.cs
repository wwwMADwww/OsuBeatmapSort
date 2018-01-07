using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    public interface IBeatmapsetDirectoryRepository<TBeatmapsetDirectory>
    {
        IEnumerable<TBeatmapsetDirectory> GetDirectories();
    }
}
