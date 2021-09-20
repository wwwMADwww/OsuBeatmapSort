using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using osuElements.Api.Repositories;
using osuElements.Api.Throttling;

namespace OsuBeatmapSort
{
    public class BeatmapsetApiChecker
    {
        private readonly string _apiKey;
        private readonly int _userId;
        private readonly TimeSpan _throttleCooldown;
        private ApiBeatmapRepository _apiBeatmap;
        private ApiScoreRepository _apiScore;

        // const int peppyUserId = 2;

        private readonly Logger _logger;
        private readonly string _logPrefix = "[ API ]";

        public BeatmapsetApiChecker(
            string apiKey,
            int userId,
            TimeSpan throttleCooldown
            )
        {
            _apiKey = apiKey;
            _userId = userId;
            _throttleCooldown = throttleCooldown;
            _logger = LogManager.GetLogger("logger");

        }



        public async Task<bool> Init()
        {
            _logger.Info($"{_logPrefix} BeatmapsetApiChecker init");

            try
            {
                TimerThrottler throttler = _throttleCooldown.TotalMilliseconds > 0
                    ? new TimerThrottler(_throttleCooldown)
                    : null;

                _apiBeatmap = new ApiBeatmapRepository(_apiKey, false, throttler);
                _apiScore = new ApiScoreRepository(_apiKey, false, throttler);

                var apiUser = new ApiUserRepository(_apiKey, false, throttler);
                var userinfo = await apiUser.Get(_userId, osuElements.GameMode.Standard);

                if (!apiUser.IsError)
                {
                    if (userinfo != null)
                    {
                        _logger.Info($"{_logPrefix} Connection test successful. Player info: ID {userinfo.UserId}, Name {userinfo.Username}, PP {userinfo.PpRaw}.");
                    }
                    else
                    {
                        _logger.Error($"{_logPrefix} Player with ID {_userId} not found.");
                        return false;
                    }

                }
                else
                {
                    _logger.Error($"{_logPrefix} Error occurred during user #{_userId} info test request: {apiUser.ApiError?.Error}");
                    return false;
                }

            }
            catch (Exception e)
            {
                _logger.Error($"Error occurred during BeatmapsetApiChecker initialization: {Environment.NewLine}{e.ToString()}");
                return false;
            }

            return true;
        }



        public async Task<BeatmapsetInfo> CheckAndPopulateAsync(BeatmapsetInfo beatmapset)
        {
            HashSet<string> notFoundMapHashes = new HashSet<string>();

            foreach (var localBeatmap in beatmapset.Beatmaps)
            {
                try
                {
                    if (localBeatmap.BeatmapId <= 0)
                    {

                        if (notFoundMapHashes.Contains(localBeatmap.GetHash()))
                            continue;

                        if (beatmapset.Id <= 0)
                        {

                            _logger.Info($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Beatmap and beatmapset has no ID, requesting from API.");

                            localBeatmap.GetHash();

                            var apimap = await _apiBeatmap.Get(localBeatmap.BeatmapHash, osuElements.GameMode.Standard);
                            if (apimap != null)
                            {
                                localBeatmap.BeatmapId = apimap.BeatmapId;
                                localBeatmap.BeatmapSetId = apimap.BeatmapSetId;
                                beatmapset.Id = apimap.BeatmapSetId;
                            }
                            else
                            {
                                if (_apiBeatmap.IsError)
                                    _logger.Warn($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Can't get beatmap by hash {localBeatmap.GetHash()} from API: {_apiBeatmap.ApiError?.Error}.");
                                else
                                    _logger.Info($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Beatmap with hash {localBeatmap.GetHash()} is not found.");
                            }
                        }
                        else
                        {

                            _logger.Info($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Beatmap has no ID, requesting from API.");

                            var apimaps = await _apiBeatmap.GetSet(beatmapset.Id, osuElements.GameMode.Standard);
                            if (apimaps?.Any() ?? false)
                            {
                                foreach (var localmap in beatmapset.Beatmaps)
                                {
                                    var apimap = apimaps.FirstOrDefault(am => am.BeatmapHash == localmap.GetHash());
                                    if (apimap != null)
                                    {
                                        localmap.BeatmapId = apimap.BeatmapId;
                                        localmap.BeatmapSetId = apimap.BeatmapSetId;
                                    }
                                    else
                                    {
                                        _logger.Info($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localmap.FileName}' Beatmap with hash {localmap.GetHash()} is not found.");
                                        notFoundMapHashes.Add(localmap.BeatmapHash);
                                    }
                                }
                            }
                            else
                            {
                                if (_apiBeatmap.IsError)
                                {
                                    _logger.Warn($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Can't get beatmaps by mapset id {beatmapset.Id} from API: {_apiBeatmap.ApiError?.Error}.");
                                }
                                else
                                {
                                    _logger.Info($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' Beatmaps for mapset id {beatmapset.Id} are not found.");
                                    break;
                                }
                            }
                        }

                    }


                    if (localBeatmap.BeatmapId > 0)
                    {
                        var score = await _apiScore.GetMapScores(localBeatmap.BeatmapId, _userId);

                        if (score != null && !_apiScore.IsError)
                        {
                            if (score.Any())
                                beatmapset.Status = BeatmapsetStatus.Played;
                        }
                        else
                        {
                            beatmapset.Status = BeatmapsetStatus.Error;
                            _logger.Warn($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' #{beatmapset.Id}/#{localBeatmap.BeatmapId} Can't get beatmap scores by map id from API: {_apiScore.ApiError?.Error}");
                        }
 
                        // if one map played then beatmap is played
                        if (beatmapset.Status == BeatmapsetStatus.Played)
                            break;
                    }
                    else
                    {
                        beatmapset.Status = BeatmapsetStatus.Error;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"{_logPrefix} '{beatmapset.DirectoryName}'/'{localBeatmap.FileName}' #{beatmapset.Id}/#{localBeatmap.BeatmapId} Error occurred during online checking beatmap." +
                        $"{Environment.NewLine}{e.ToString()}");
                }

            } // /foreach bi.Beatmaps



            return beatmapset;
        }

    }
}
