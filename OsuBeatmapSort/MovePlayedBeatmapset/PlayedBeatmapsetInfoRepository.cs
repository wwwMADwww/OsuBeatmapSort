using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;
using osuElements.Api;
using osuElements.Api.Repositories;
using osuElements.Beatmaps;
using osuElements.Db;

namespace OsuBeatmapSort.MovePlayedBeatmapset
{
    
    public class PlayedBeatmapsetInfoRepository : IBeatmapsetInfoRepository<string, BeatmapsetInfo>
    {

        #region class ProcessChannels
        class ProcessChannels
        {
            public BufferBlock<string> DirsBuffer {get;set;}
            public BufferBlock<IEnumerable<Beatmap>> ApiBuffer {get;set;}
            public BufferBlock<BeatmapsetInfo> ResultBuffer {get;set;}

            private int _dirTaskCount = 0;

            // чтение 32 битных значений всегда атомарно на 32+ битных платформах
            public int DirWorkingTasksCount { get => _dirTaskCount; }

            public void IncrementDirWorkingTasksCount() => Interlocked.Increment(ref _dirTaskCount);
            public void DecrementDirWorkingTasksCount() => Interlocked.Decrement(ref _dirTaskCount);


            private int _apiTaskCount = 0;

            // чтение 32 битных значений всегда атомарно на 32+ битных платформах
            public int ApiWorkingTasksCount { get => _apiTaskCount; }

            public void IncrementApiWorkingTasksCount() => Interlocked.Increment(ref _apiTaskCount);
            public void DecrementApiWorkingTasksCount() => Interlocked.Decrement(ref _apiTaskCount);


        }

        #endregion ProcessChannels

        // хеши сыгранных карт из osu!.db
        IEnumerable<string> _playedMapsHashes;

        private readonly int _userID;
        private readonly int _tasksCount;
        private readonly Logger _logger;

        
        public PlayedBeatmapsetInfoRepository(
            int userID,
            string apikey,
            int tasksCount,
            string osuDbFilename
            )
        {
            ApiBeatmapRepository.ThrowExceptions = true;

            _userID = userID;
            ApiRepositoryBase.Key = apikey;
            _tasksCount = tasksCount;

            _logger = LogManager.GetLogger("logger");

            osuDbFilename = Path.GetFullPath(osuDbFilename);
            _logger.Info($"Reading osu database file '{osuDbFilename}'...");

            var osudb = new OsuDb();
            osudb.FullPath = osuDbFilename;
            osudb.ReadFile();
            
            _logger.Info($"Reading beatmap hashes...");

            _playedMapsHashes = new HashSet<string>(osudb.Beatmaps
                .Where(b => !b.Unplayed)
                .Select(b => b.BeatmapHash)
                .ToArray()
            );

            _logger.Info($"Reading beatmap hashes complete.");
        }
        
        public BufferBlock<BeatmapsetInfo> GetBeatmapsetInfo(BufferBlock<string> beatmapsetDirsBuffer)
        {            
            var channels = new ProcessChannels()
            {
                DirsBuffer = beatmapsetDirsBuffer,
                ApiBuffer = new BufferBlock<IEnumerable<Beatmap>>(new DataflowBlockOptions(){BoundedCapacity = _tasksCount * 10}),
                ResultBuffer = new BufferBlock<BeatmapsetInfo>(new DataflowBlockOptions(){BoundedCapacity = _tasksCount * 10})
            };

            Task.Factory.StartNew(() => ManageTasks(channels));

            return channels.ResultBuffer;
        }


        // TODO: разнести по отдельным стратегиям всё что ниже
        async Task ManageTasks(ProcessChannels channels)
        {
            
            _logger.Trace($"Starting managing task.");

            var processTasks = new Task[_tasksCount];
            for(int i = 0; i < processTasks.Length; i++)
            {
                processTasks[i] = Task.Factory.StartNew(async () => await ProcessDirs(channels));
                channels.IncrementDirWorkingTasksCount();
            }

            var apiTasks = new Task[_tasksCount];
            for(int i = 0; i < apiTasks.Length; i++)
            {
                apiTasks[i] = Task.Factory.StartNew(async () => await ProcessBeatmapsUsingApi(channels));
                channels.IncrementApiWorkingTasksCount();
            }

            _logger.Info($"Waiting for all tasks.");
            // Task.WaitAll(processTasks.Concat(apiTasks).ToArray());

            while (channels.DirWorkingTasksCount > 0) 
            {
                await Task.Delay(1000);
            }

            _logger.Info($"Closing api channel.");
            channels.ApiBuffer.Complete();

            while (channels.ApiWorkingTasksCount > 0) 
            {
                await Task.Delay(1000);
            }

            _logger.Info($"Closing beatmap set info channel.");
            channels.ResultBuffer.Complete();
        }
        
        async Task ProcessDirs(ProcessChannels channels)
        {            
               
            _logger.Trace($"Start processing dirs.");

            while (true)
            {

                // для сообщений об ошибках
                string currentDir = null;
                string currentFile = null;

                try
                {
                    currentDir = null;
                    currentFile = null;

                    if (!await channels.DirsBuffer.OutputAvailableAsync())
                    {
                        _logger.Trace($"Dir channel closed.");
                        break;
                    }

                    // currentDir = await channels.DirsBuffer.ReceiveAsync();

                    if (!channels.DirsBuffer.TryReceive(x => true, out currentDir))
                        continue;
                    
                    if (!Directory.Exists(currentDir))
                    {
                        _logger.Warn($"Beatmapset dir does not exists: '{currentDir}'.");
                        break;
                    }
                    
                    var beatmapFilenames = Directory.EnumerateFiles(currentDir, "*.osu", SearchOption.TopDirectoryOnly);
                    
                    if (!beatmapFilenames.Any())
                    {
                        _logger.Warn($"Beatmapset dir does not contains map files: '{currentDir}'.");
                        break;
                    }

                    
                    _logger.Trace($"Processing dir: '{currentDir}'.");

                    // флаг сыгранности карты
                    bool isBeatmapPlayed = false;

                    var notFoundMaps = new List<Beatmap>();

                    foreach (var beatmapFilename in beatmapFilenames)
                    {
                        currentFile = beatmapFilename;

                        
                        _logger.Trace($"Processing beatmap: '{beatmapFilename}'.");

                        var localBeatmap = new Beatmap(beatmapFilename);
                                                
                        if (_playedMapsHashes.Contains(localBeatmap.GetHash()))
                        {
                            _logger.Info($"Beatmap '{localBeatmap.ToString()}' found in osu local database as played.");
                            isBeatmapPlayed = true;
                            
                            // если хоть одна карта из мапсета сыграна, значит мапсет играли, остальные карты можно не проверять                            
                            break;
                        }
                        
                        // информации о сыгранности карты или информации о карте вообще в базе может не быть,
                        // поэтому проверяем наличие скоров в онлайне
                        notFoundMaps.Add(localBeatmap);
                    }

                    if (isBeatmapPlayed) 
                        await channels.ResultBuffer.SendAsync(new BeatmapsetInfo() { Directory = currentDir, IsPlayed = true });
                    else
                        await channels.ApiBuffer.SendAsync(notFoundMaps.ToArray());

                    // notFoundMaps.Clear();

                }
                catch (Exception e)
                {
                    _logger.Error($"Error occured during getting mapset info: dir '{currentDir}', file '{currentFile}'"+
                        $"{Environment.NewLine}{e.ToString()}");
                }


            } // /while

            _logger.Trace($"End processing dirs.");
            channels.DecrementDirWorkingTasksCount();
        }


        async Task ProcessBeatmapsUsingApi(ProcessChannels channels)
        {                        
            
            _logger.Trace($"Start getting beatmap info from API.");

            // TODO: реализовать троттлинг, API разрешает не более 60 запросов в минуту
            IApiBeatmapRepository apibeatmaprepo = null;
            
            try
            {
                apibeatmaprepo = new ApiBeatmapRepository();
            }
            catch (Exception e)
            {
                _logger.Error($"Error occured during getting beatmap info from API: {Environment.NewLine}{e.ToString()}");
                return;
            }

            while (true)
            {

                // для сообщений об ошибках
                Beatmap currentLocalBeatmap = null;

                try
                {
                    IEnumerable<Beatmap> localBeatmaps = null;
                    currentLocalBeatmap = null;

                    if (!await channels.ApiBuffer.OutputAvailableAsync())
                    {
                        _logger.Trace($"Api channel closed.");
                        break;
                    }

                    // localBeatmap = await channels.ApiBuffer.ReceiveAsync();

                    if (!channels.ApiBuffer.TryReceive(x => true, out localBeatmaps))
                        continue;

                    if ((localBeatmaps?.Any() ?? false) == false)
                    {
                        _logger.Warn($"Received empty beatmaps collection.");
                        continue;
                    }
                    
                    bool? isBeatmapsetPlayed = null;

                    foreach(var localBeatmap in localBeatmaps)
                    {

                        currentLocalBeatmap = localBeatmap;

                        _logger.Trace($"Api processing '{localBeatmap.FullPath}'.");


                        // в старых картах ID может не быть, поэтому получаем этот ID по хешу файла карты
                        if (localBeatmap.BeatmapId == 0)
                        {
                            // на всякий случай считаем хеш, чтобы он точно был в поле BeatmapHash
                            localBeatmap.GetHash();

                            var apiBeatmap = await apibeatmaprepo.Get(localBeatmap.BeatmapHash);
                            
                            if (apibeatmaprepo.IsError)
                            {
                                _logger.Warn($"Api method Get returned error: "+  
                                    $"Beatmap: '{localBeatmap.ToString()}', BeatmapHash '{localBeatmap.BeatmapHash}', error '{apibeatmaprepo.ApiError}'");
                            }
                            else
                            {
                                localBeatmap.BeatmapId = apiBeatmap?.BeatmapId ?? 0;
                            }

                            // await localBeatmap.AddApiProperties();
                        }
                        
                        if (localBeatmap.BeatmapId != 0)
                        {
                            var apiScore = await apibeatmaprepo.GetScores(localBeatmap.BeatmapId, _userID);
                            
                            if (apibeatmaprepo.IsError)
                            {
                                _logger.Warn($"Api method GetScores returned error: "+ 
                                    $"Beatmap: '{localBeatmap.ToString()}', BeatmapId {localBeatmap.BeatmapId}, error '{apibeatmaprepo.ApiError}'");
                            }

                            // здесь тоже возвращается null при сетевых ошибках
                            if (apiScore != null)
                            {
                                isBeatmapsetPlayed = apiScore.Any();
                                _logger.Info($"Got online beatmap scores. "+
                                    $"Beatmap: '{localBeatmap.ToString()}', BeatmapId: {localBeatmap.BeatmapId}, isBeatmapsetPlayed: {isBeatmapsetPlayed}.");
                            }
                            else
                            {
                                _logger.Warn($"Api method GetScores returned null. "+
                                    $"Beatmap: '{localBeatmap.ToString()}', ID: {localBeatmap.BeatmapId}.");
                            }
                            
                            // если хоть одна карта из мапсета сыграна, значит мапсет играли, остальные карты можно не проверять   
                            if (isBeatmapsetPlayed == true)
                                break;

                        }
                        else 
                        {
                            // wtf
                        }

                    } // /for

                    await channels.ResultBuffer.SendAsync(new BeatmapsetInfo() { 
                        Directory = localBeatmaps.First().Directory, 
                        IsPlayed = isBeatmapsetPlayed 
                    });
                
                }
                catch (Exception e)
                {
                    _logger.Error($"Error occured during getting mapset info from api:{Environment.NewLine}{e.ToString()}");
                }

            } // /while

            _logger.Trace($"End getting mapset info from api.");            
            channels.DecrementApiWorkingTasksCount();
        }

    }



}
