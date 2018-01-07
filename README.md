# OsuBeatmapsetProcessor

Утилита для массовой обработки мапсетов osu.

Скомпилированный дистрибутив находится в папке [distr](https://github.com/wwwMADwww/OsuBeatmapsetProcessor/tree/master/distr) или по прямой ссылке - [клик](https://github.com/wwwMADwww/OsuBeatmapsetProcessor/raw/master/distr/OsuBeatmapsetProcessor.zip).

## Параметры запуска утилиты:

    OsuBeatmapsetProcessor.exe [--help] <имя процесса>
    OsuBeatmapsetProcessor.exe <имя процесса> [параметры процесса]
    
      Первый параметр - имя процесса. (Сейчас он один - MovePlayedBeatmapset)
      
      Параметры для процесса MovePlayedBeatmapset:
      
        --apikey               - личный ключ доступа к osu!api. Получить можно здесь https://osu.ppy.sh/p/api.
        
        --playerid             - идентификатор игрока (не ник). Получить его можно из ссылки на свой профиль.
        
        --ThreadCount          - количество потоков обработки. По умолчанию = 16.
        
        [--OsuDbFilename]      - путь до файла osu!.db. По умолчанию = "osu!.db".
        
        [--SongsDir]           - путь до папки с мапсетами. По умолчанию = "Songs".
        
        [--mapsetPlayedDir]    - путь до папки для сыгранных мапсетов. По умолчанию = "songsPlayed".
        
        [--mapsetNotPlayedDir] - путь до папки для несыгранных мапсетов. По умолчанию = "songsNotPlayed".

## Пример запуска:

    OsuBeatmapsetProcessor.exe MovePlayedBeatmapset --apikey=0102030405060708090a0b0c0d0e0f1011121314 --playerid=1337

## Процесс MovePlayedBeatmapset:

Перемещает мапсеты из папки `SongsDir` в папку `mapsetPlayedDir` (если были попытки пройти хоть одну карту из мапсета) или `mapsetNotPlayedDir` (если не было таких попыток ни на одной карте из мапсета). Наличие попыток пройти карту определяется по локальной базе osu `OsuDbFilename` и онлайн наблице рекордов.
