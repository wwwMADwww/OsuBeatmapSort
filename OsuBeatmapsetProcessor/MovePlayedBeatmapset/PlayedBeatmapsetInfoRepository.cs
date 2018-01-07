using NLog;
using osuElements.Api;
using osuElements.Api.Repositories;
using osuElements.Beatmaps;
using osuElements.Db;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor.MovePlayedBeatmapset
{
    
    public class PlayedBeatmapsetInfoRepository : IBeatmapsetInfoRepository<string, BeatmapsetInfo>
    {

        IEnumerable<string> _playedMapsHashes;

        private readonly int _userID;
        private readonly int _threadCount;
        private readonly string _osuDbFilename;
        private readonly Logger _logger;

        public PlayedBeatmapsetInfoRepository(
            int userID,
            string apikey,
            int threadCount,
            string osuDbFilename,
            Logger logger
            )
        {
            _userID = userID;
            ApiRepositoryBase.Key = apikey;
            _threadCount = threadCount;
            _osuDbFilename = osuDbFilename;
            _logger = logger;
        }
        
        public IEnumerable<BeatmapsetInfo> GetBeatmapsetInfoRepository(IEnumerable<string> beatmapsetDirs)
        {
            _logger.Info($"Preparing to get beatmapset info.");
            
            string osuDbFilename = Path.GetFullPath(_osuDbFilename);
            _logger.Info($"Parse osu database file '{osuDbFilename}'.");

            var osudb = new OsuDb();
            osudb.FileName = osuDbFilename;
            osudb.ReadFile();
            
            _playedMapsHashes = osudb.Beatmaps
                .Where(b => !b.Unplayed)
                .Select(b => b.BeatmapHash)
                .ToArray();


            _logger.Info($"Start getting beatmapset info.");

            var beatmapInfoList = beatmapsetDirs
                .AsParallel()
                .WithDegreeOfParallelism(_threadCount)
                .Select(d => ProcessDir(d).Result)
                .ToArray();

            _logger.Info($"End getting beatmapset info.");

            return beatmapInfoList;
        }
        
        async Task<BeatmapsetInfo> ProcessDir(string dir)
        {
            var res = new BeatmapsetInfo() { Directory = dir, IsPlayed = null };
            
            int threadid = Thread.CurrentThread.ManagedThreadId;
            
            var apibeatmaprepo = new ApiBeatmapRepository();

            while (true)
            {
                try
                {

                    _logger.Info($"{threadid}| Getting beatmapset info '{dir}'.");
                    
                    if (!Directory.Exists(dir))
                    {
                        _logger.Info($"{threadid}| Beatmapset dir does not exists: '{dir}'.");
                        break;
                    }
                    
                    var beatmapFilenames = Directory.EnumerateFiles(dir, "*.osu", SearchOption.TopDirectoryOnly);
                    
                    if (!beatmapFilenames.Any())
                    {
                        _logger.Info($"{threadid}| Beatmapset dir does not contains map files: '{dir}'.");
                        break;
                    }

                    // флаг сыгранности карты. null говорит об отсутствии информации по каким-либо причинам
                    bool? isBeatmapPlayed = null;

                    foreach (var beatmapFilename in beatmapFilenames)
                    {
                        _logger.Info($"{threadid}| Getting beatmap info '{beatmapFilename}'.");
                        
                        var localBeatmap = new Beatmap(beatmapFilename);
                        
                        string beatmapArtistTitle = $"{localBeatmap.Artist} - {localBeatmap.Title}";

                        // запоминаем хеш карты, чтобы больше не обращаться к файлу на диске
                        string beatmapHash = localBeatmap.GetHash();

                        // Заставляем сборщик мусора очистить все ненужные ресурсы, 
                        // здесь это сделано для того чтобы прочитанный ранее файл карты точно закрылся
                        GC.Collect();
                        
                        if (_playedMapsHashes.Contains(beatmapHash))
                        {
                            _logger.Info($"{threadid}| Beatmap '{beatmapArtistTitle}' found in osu local database as played.");
                            isBeatmapPlayed = true;
                        }
                        
                        if (isBeatmapPlayed != true)
                        {
                            int beatmapid = localBeatmap.BeatmapId;

                            // в старых картах ID может не быть, поэтому получаем этот ID по хешу файла карты
                            if (beatmapid == 0)
                            {
                                // репозиторий возвращает null при сетевых ошибках 
                                // (вернее, так задумано, на данный момент вообще exception возникает из-за ошибки в коде библиотеки) 
                                // и при отсутствии карты в онлайне тоже возвращается null, непонятно как воспринимать такой ответ, 
                                // считаем null отсутствием информации.
                                var apiBeatmap = await apibeatmaprepo.Get(beatmapHash);
                                beatmapid = apiBeatmap?.BeatmapId ?? 0;
                                _logger.Info($"{threadid}| Got online beatmap by hash. Beatmap: '{beatmapArtistTitle}', ID: {beatmapid}");
                            }
                            
                            if (beatmapid != 0)
                            {
                                var apiScore = await apibeatmaprepo.GetScores(beatmapid, _userID);

                                // здесь тоже возвращается null при сетевых ошибках
                                if (apiScore != null)
                                {
                                    isBeatmapPlayed = apiScore.Any();
                                    _logger.Info($"{threadid}| Got online beatmap scores. Beatmap: '{beatmapArtistTitle}', ID: {beatmapid}, isBeatmapPlayed: {isBeatmapPlayed}.");
                                }
                                else
                                {
                                    _logger.Warn($"{threadid}| Api method GetScores returned null. Beatmap: '{beatmapArtistTitle}', ID: {beatmapid}.");
                                }
                            }
                        }


                        // если хоть одна карта из мапсета сыграна, значит мапсет играли, остальные карты можно не проверять
                        if (isBeatmapPlayed == true)
                            break;
                    }

                    res.IsPlayed = isBeatmapPlayed;

                }
                catch (Exception e)
                {
                    _logger.Error($"{threadid}| Error occured while getting mapset info '{dir}':{Environment.NewLine}{e.ToString()}");
                }

                break;
            }
            _logger.Info($"{threadid}| End getting mapset info '{dir}'. ThreadID: {threadid}.");

            return res;
        }

    }



}
