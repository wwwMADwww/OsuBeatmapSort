# OsuBeatmapsetProcessor

Утилита для массовой обработки мапсетов osu.

Для скачивания дистрибутива перейдите на страницу релизов - [ссылка](https://github.com/wwwMADwww/OsuBeatmapsetProcessor/releases).

Для запуска требуется наличие установленной среды выполнения .NET Core 2.2 или выше - [ссылка](https://dotnet.microsoft.com/download)

## Общий синтаксис запуска утилиты:

    dotnet OsuBeatmapsetProcessor.dll [--help] <имя процесса>
    dotnet OsuBeatmapsetProcessor.dll <имя процесса> [Параметры процесса]
    
## Процесс MovePlayedBeatmapset:

Перемещает мапсеты из папки `SongsDir` в папку `mapsetPlayedDir` (если были попытки пройти хоть одну карту из мапсета) или `mapsetNotPlayedDir` (если не было таких попыток ни на одной карте из мапсета). Наличие попыток пройти карту определяется по локальной базе osu `OsuDbFilename` и онлайн таблице рекордов.

### Аргументы:

    dotnet OsuBeatmapsetProcessor.dll [--help] MovePlayedBeatmapset
    dotnet OsuBeatmapsetProcessor.dll MovePlayedBeatmapset [Параметры]
      
      Параметры:
      
        --apikey               - личный ключ доступа к osu!api. Получить можно здесь https://osu.ppy.sh/p/api.
        
        --playerid             - идентификатор игрока (не ник). Получить его можно из ссылки на свой профиль.
        
        [--OsuDbFilename]      - путь до файла osu!.db. По умолчанию = "osu!.db".
        
        [--SongsDir]           - путь до папки с мапсетами. По умолчанию = "Songs".
        
        [--mapsetPlayedDir]    - путь до папки для сыгранных мапсетов. По умолчанию = "songsPlayed".
        
        [--mapsetNotPlayedDir] - путь до папки для несыгранных мапсетов. По умолчанию = "songsNotPlayed".
        
        [--TasksCount]         - количество асинхронных операций обработки. По умолчанию = 20.

### Примеры запуска:

    dotnet OsuBeatmapsetProcessor.dll MovePlayedBeatmapset --apikey=0102030405060708090a0b0c0d0e0f1011121314 --playerid=1337

    dotnet OsuBeatmapsetProcessor.dll MovePlayedBeatmapset --apikey=0102030405060708090a0b0c0d0e0f1011121314 --playerid=1337 --osudbfilename D:\GAMES\osu!\osu!.db --songsdir D:\GAMES\osu!\songs --mapsetplayeddir D:\GAMES\osu!\SongsPlayed --mapsetnotplayeddir D:\GAMES\osu!\SongsNotPlayed

