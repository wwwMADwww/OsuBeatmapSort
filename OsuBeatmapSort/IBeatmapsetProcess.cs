using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapSort
{
    public interface IBeatmapsetProcess
    {
        Task Process();
    }

    public interface IBeatmapsetProcessFactory
    {
        IBeatmapsetProcess Create();
    }
    

}