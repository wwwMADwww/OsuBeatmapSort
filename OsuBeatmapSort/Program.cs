using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace OsuBeatmapSort
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {

            var logger = LogManager.GetLogger("logger");

            try
            {
                if (!AppOptionsParser.Parse(out var appOptions))
                {
                    Console.WriteLine("Press enter to exit;");
                    Console.ReadLine();
                    return 1;
                }


                string BasedPath(string basePath, string relativePath)
                {
                    if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(relativePath))
                        return relativePath;
                    else
                        return Path.Combine(basePath, relativePath);
                }

                logger.Info("Creating components");

                var mapsetProvider = new BeatmapsetProvider(BasedPath(appOptions.dirGame, appOptions.DirSongs));

                var mapsetLocalChecker = new BeatmapsetLocalDbChecker(
                    BasedPath(appOptions.dirGame, appOptions.OsuDbFilename),
                    BasedPath(appOptions.dirGame, appOptions.DirSongs));


                var apiChecker = new BeatmapsetApiChecker(
                    appOptions.ApiKey,
                    appOptions.PlayerId,
                    TimeSpan.FromSeconds(appOptions.ApiRpm > 0 ? (60.0f / appOptions.ApiRpm) : 0)
                    );

                var mapsetMoveStrat = new BeatmapsetMoveStrategy(
                    new Dictionary<BeatmapsetStatus, string>() {
                        { BeatmapsetStatus.Played, BasedPath(appOptions.dirGame, appOptions.DirPlayed) },
                        { BeatmapsetStatus.NotPlayed, BasedPath(appOptions.dirGame,appOptions.DirNotPlayed) },
                        { BeatmapsetStatus.Error, BasedPath(appOptions.dirGame,appOptions.DirError) },
                    },
                    BasedPath(appOptions.dirGame, appOptions.DirSongs));


                var processor = new Processor(
                    mapsetProvider,
                    mapsetLocalChecker,
                    apiChecker,
                    mapsetMoveStrat,
                    appOptions.ApiThreads
                    );

                logger.Info("Initializing components and process");


                if (!await processor.Init())
                {
                    logger.Error($"Process initialization failed.");
                }
                else
                {
                    logger.Info("Starting process");

                    var proc = processor.Process();

                    logger.Info("Process started");

                    var res = await proc;

                    logger.Log(res ? LogLevel.Info : LogLevel.Warn, $"Process finished {(res ? "successfully" : "with error")}.");
                }
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                    
            }
            catch (Exception e)
            {
                logger.Fatal("Error occurred during initialization or processing: " +
                    Environment.NewLine + e.ToString());
                throw;
            }

            return 0;
        }
        

    }

}
