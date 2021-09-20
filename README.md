# OsuBeatmapSort

This console utility moves played osu mapsets to one dir and not played mapsets to another dir. 

Beatmap set considered as played if any map in it was played. Map status is determined by special flag in `osu!.db` or by presence of any score specified player have on this map in online leaderboard.

## Download and run

To download binary executables please go to [Releases](https://github.com/wwwMADwww/OsuBeatmapSort/releases) page.

This app requires .NET 5 or higher, please [download](https://dotnet.microsoft.com/download) and install it if you haven't yet.

This app uses osu!api which requires API Key, you can get it from [this](https://osu.ppy.sh/p/api/) page.

## Commandline params:

To see this list you can just run the app without any params.

```
  --apiKey          Required. osu!api access key.

  --apiRpm          (Default: 0) Maximum requests to osu!api per minute. Zero means no limit.

  --apiThreads      (Default: 8) Number of threads for requesting info from osu!api. Useless if apiRpm is set.

  --playerId        Required. Player ID (ID, not nickname)

  --dbFilename      (Default: osu!.db) Path to osu!.db file.

  --dirGame         Required. Path to the game directory. Will be used as base path for any relative path.

  --dirSongs        (Default: Songs) Path to beatmapsets directory.

  --dirPlayed       (Default: SongsPlayed) Dir for played beatmapsets. '?' means that mapsets will not be moved.

  --dirNotPlayed    (Default: SongsNotPlayed) Dir for not played beatmapsets. '?' means that mapsets will not be moved.

  --dirError        (Default: SongsError) Dir for beatmapsets during processing which error is occurred. '?' means that
                    mapsets will not be moved.

  --help            Display this help screen.

  --version         Display version information.
```


## Run examples

---

`OsuBeatmapSort --apiKey=00112233445566778899aabbccddeeff00112233 --playerId=12345678 --dirGame="D:\GAMES\osu!"` 

App will 
- scan mapsets in `D:\GAMES\osu!\Songs`
- move played mapsets to `D:\GAMES\osu!\SongsPlayed`
- move not played mapsets to `D:\GAMES\osu!\SongsNotPlayed`
- move mapsets with error to `D:\GAMES\osu!\SongsError`

---

`OsuBeatmapSort --apiKey=00112233445566778899aabbccddeeff00112233 --playerId=12345678 --dirGame="D:\GAMES\osu!" --dirPlayed="played" --dirNotPlayed="D:\notplayed" --dirError=?`

App will
- scan mapsets in `D:\GAMES\osu!\Songs`
- move played mapsets to `D:\GAMES\osu!\played`
- move not played mapsets to `D:\notplayed`
- leave mapsets with error in `D:\GAMES\osu!\Songs`

---
