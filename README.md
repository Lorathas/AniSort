# AniSort
AniSort is a command line program for organizing files that uses the AniDB UDP API for fetching info for a file.

The solution is split into the three following projects.  
## AniDbSharp 
This project contains code for interfacing with the AniDB UDP API. Eventually I'd like to split this off into it's own separate repository and NuGet package, but until the functionality is more mature it will just live here.

## AniSort.Core
Core functionality of the console program is put here. Going forwards I would like to add a WPF project that provides similar functionality to the console program so any functionality that would be useful in both the console and the GUI will be stored here.


## AniSort
The console program itself that uses functionality from the AniSort.Core and AniDbSharp projects to provide a console program for sorting anime. The program supports both command line arguments and a config file.  

### File Organization
To control the naming of files you can provide a custom format string such as  
`{animeRomaji}\{animeRomaji} - {episodeNumber} - {episodeEnglish}[{subGroupShort}][{resolution}][{videoCodec}][{crc32}]`  
which would output  
`Koukaku Kidoutai S.A.C. 2nd GIG\Koukaku Kidoutai S.A.C. 2nd GIG - 02 - Night Cruise[Hi10][1280x688][h264][D7083952]`  
as the folder and filename.

The following variables are available to use in the format strings:  
* `animeEnglish` - Anime title in English
* `animeRomaji` - Anime title in Japanese, romanized to Latin characters
* `animeKanji` - Anime title in Kanji
* `episodeNumber` - Episode number that will be zero padded based on the number of epsiodes a series has
* `fileVersion` - Version number of the file if greater than 1
* `episodeEnglish` - Episode title in English
* `episodeRomaji` - Episode title in Japanese, romanized to Latin characters
* `episodeKanji` - Episode title in Kanji
* `subGroup` - Full sub/release group name
* `subGroupShort` - Abbreviated sub/release group name
* `resolution` - Video resolution for the file
* `videoCodec` - Video codec for the file
* `crc32` - CRC32 hash of the file in hex
* `sha1` - SHA-1 hash of the file in hex
* `ed2k` - eD2k hash of the file in hex

You can also add conditionally rendered prefixes and suffixes to a variable by starting or ending a variable block with apostrophes. So as an example if you wanted the file version to be prefixed with v you could do a format string of `{episodeNumber}{'v'fileVersion}` to output `02v2` for the above example file assuming the file version is the second version. Since `{fileVersion}` is only emitted to the path when it is greater than 1 the same format string would render `02` for version one of the file.

If you want to use curly braces in your paths for whatever reason they can be escape them by using two of whichever curly brace you are trying to use.