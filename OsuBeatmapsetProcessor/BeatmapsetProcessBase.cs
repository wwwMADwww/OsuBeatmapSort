using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    public class BeatmapsetProcessBase<TBeatmapsetDirectory, TBeatmapsetInfo> : IBeatmapsetProcess
    {
        private readonly IBeatmapsetDirectoryRepository<TBeatmapsetDirectory> _beatmapsetDirectoriesListStrategy;
        private readonly IBeatmapsetInfoRepository<TBeatmapsetDirectory, TBeatmapsetInfo> _beatmapsetInfoRepository;
        private readonly IBeatmapsetProcessStrategy<TBeatmapsetInfo> _beatmapsetProcessStrategy;

        public BeatmapsetProcessBase(
            IBeatmapsetDirectoryRepository<TBeatmapsetDirectory> beatmapsetDirectoriesListStrategy,
            IBeatmapsetInfoRepository<TBeatmapsetDirectory, TBeatmapsetInfo> beatmapsetInfoRepository,
            IBeatmapsetProcessStrategy<TBeatmapsetInfo> beatmapsetProcessStrategy
            )
        {
            _beatmapsetDirectoriesListStrategy = beatmapsetDirectoriesListStrategy;
            _beatmapsetInfoRepository = beatmapsetInfoRepository;
            _beatmapsetProcessStrategy = beatmapsetProcessStrategy;
        }

        public async Task Process()
        {
            var beatmapDirectoryList = _beatmapsetDirectoriesListStrategy.GetDirectories();
            var beatmapInfoList = _beatmapsetInfoRepository.GetBeatmapsetInfo(beatmapDirectoryList);
            await _beatmapsetProcessStrategy.Process(beatmapInfoList);
        }
    }


}