using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using NLog;

namespace OsuBeatmapSort
{
    public class Processor
    {
        private readonly BeatmapsetProvider _beatmapsetProvider;
        private readonly BeatmapsetLocalDbChecker _beatmapsetLocalDbChecker;
        private readonly BeatmapsetApiChecker _beatmapsetApiChecker;
        private readonly BeatmapsetMoveStrategy _beatmapsetMoveStrategy;
        private readonly int _apiParallelism;

        private readonly Logger _logger;
        private readonly string _logPrefix = "[Proc ]";

        private ActorSystem _actorSystem;
        private ActorMaterializer _materializer;
        private RunnableGraph<NotUsed> _graph;
        private TaskCompletionSource<bool> _graphCompleted;

        const int portMove = 0;
        const int portCheckOnline = 1;

        public Processor(
            BeatmapsetProvider beatmapsetProvider,
            BeatmapsetLocalDbChecker beatmapsetLocalDbChecker,
            BeatmapsetApiChecker beatmapsetApiChecker,
            BeatmapsetMoveStrategy beatmapsetMoveStrategy,
            int apiParallelism)
        {
            _beatmapsetProvider = beatmapsetProvider;
            _beatmapsetLocalDbChecker = beatmapsetLocalDbChecker;
            _beatmapsetApiChecker = beatmapsetApiChecker;
            _beatmapsetMoveStrategy = beatmapsetMoveStrategy;
            _apiParallelism = apiParallelism;
            _logger = LogManager.GetLogger("logger");
        }


        public async Task<bool> Init()
        {
            try
            {
                if (!_beatmapsetProvider.Init())
                    return false;

                if (!_beatmapsetLocalDbChecker.Init())
                    return false;

                if (!_beatmapsetMoveStrategy.Init())
                    return false;

                if (!await _beatmapsetApiChecker.Init())
                    return false;

                CreateGraph();

            }
            catch (Exception e)
            {
                _logger.Error($"{_logPrefix} Error occurred during processor initialization: {Environment.NewLine}{e.ToString()}");
                return false;
            }

            return true;
        }


        protected void CreateGraph()
        {
            _actorSystem = ActorSystem.Create("system");
            _materializer = _actorSystem.Materializer();

            _graphCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);



            var mapsetSource = Source.From(_beatmapsetProvider.GetMapsets());

            var mapsetLocalFlow = Flow.FromFunction((BeatmapsetInfo bi) => _beatmapsetLocalDbChecker.CheckAndPopulate(bi));

            var mapsetPartition = new Partition<BeatmapsetInfo>(2, b =>
                b.Status == BeatmapsetStatus.NotPlayed
                    ? portCheckOnline
                    : portMove
            );



            var mapsetApiBuf = Flow
                .Identity<BeatmapsetInfo>()
                .Buffer(10000, OverflowStrategy.Backpressure);

            var mapsetApiFlow = Flow
                .Create<BeatmapsetInfo>()
                .SelectAsync(_apiParallelism, async bi => await _beatmapsetApiChecker.CheckAndPopulateAsync(bi));

            var mapsetMove = Flow
                .FromFunction<BeatmapsetInfo, BeatmapsetInfo>(bi =>
                {
                    _beatmapsetMoveStrategy.Move(bi);
                    return bi;
                })
                .WatchTermination(async (_, t) =>
                {
                    try
                    {
                        await t;
                        _graphCompleted.SetResult(true);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"{_logPrefix} Error occurred in graph: {Environment.NewLine}{e.ToString()}");
                        _graphCompleted.SetResult(false);
                    }
                    return _materializer;
                })
                .To(Sink.Ignore<BeatmapsetInfo>());



            _graph = RunnableGraph.FromGraph(GraphDsl.Create(b =>
            {
                var mapsetPartitionShape = b.Add(mapsetPartition);

                var mapsetMoveMergeShape = b.Add(new Merge<BeatmapsetInfo>(2));


                b.From(mapsetSource)
                    .Via(mapsetLocalFlow)
                    .To(mapsetPartitionShape);


                b.From(mapsetPartitionShape.Out(portMove))
                    .To(mapsetMoveMergeShape);

                b.From(mapsetPartitionShape.Out(portCheckOnline))
                    .Via(mapsetApiBuf)
                    .Via(mapsetApiFlow)
                    .To(mapsetMoveMergeShape);


                b.From(mapsetMoveMergeShape)
                    .To(mapsetMove);

                //
                //              mapsetSource
                //                   │
                //                   ▼
                //            mapsetLocalFlow
                //                   │
                //                   ▼
                //         ┌─ mapsetPartition ──┐
                //         │                    │
                //  portCheckOnline         portMove
                //         │                    │
                //         ▼                    │
                //    mapsetApiBuf              │
                //         │                    │
                //         ▼                    │
                //   mapsetApiFlow              │
                //         │                    │
                //         └► bmsMoveSinkMerge ◄┘
                //                   │
                //                   ▼
                //               mapsetMove
                //

                return ClosedShape.Instance;

            }));


        }


        public async Task<bool> Process()
        {
            _graph.Run(_materializer);

            return await _graphCompleted.Task;

        }


    }
}
