# AniSort [![.NET](https://github.com/Lorathas/AniSort/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Lorathas/AniSort/actions/workflows/dotnet.yml)
AniSort is a command line program for organizing files that uses the AniDB UDP API for fetching info for a file.

The solution is split into the four following projects.  
## AniDbSharp 
This project contains code for interfacing with the AniDB UDP API. Eventually I'd like to split this off into it's own separate repository and NuGet package, but until the functionality is more mature it will just live here.

## AniSort.Core
Core functionality of the console program is put here. Going forwards I would like to add a WPF project that provides similar functionality to the console program so any functionality that would be useful in both the console and the GUI will be stored here.


## AniSort.Cli
The console program itself that uses functionality from the AniSort.Core and AniDbSharp projects to provide a console program for sorting anime. The program supports both command line arguments and a config file.

## AniSort
A consoleless version of the program that works the exact same way as AniSort.Cli.

# Commands
There are several commands and various maintenance tasks defined for usage in the program. Maintenance commands are called via `maint {command}`.  

`sort` will hash and search for matches for the files and then move them if found  
`hash` will hash the files found in the search paths and add them to the database  
`maint` will run all available maintenance tasks  
`maint resfix` will fix any files that have a resolution of 0x0 in their filename which tends to cause issues with Taiga's filename parsing. By default the program will check this info when sorting if AniDb doesn't have the resolution info already  
`maint upgradetodb` will import the original flat file data into the sqlite database for use with the newer version

# Config
Config files can be stored in one of three formats: json, xml, and yaml.
By default the program will look for them in the users home directory (`~` or `%USERPROFILE%`) as either `AniSort.{extension}` or `anisort.{extension}`,
but you can also provide a path to a different location with the `--config` command line option.  
Samples of each config file type can be found in the root folder of `AniSort.Core`.  
NOTE: Field names in XML need to be `PascalCase` and names in JSON and YAML need to be `camelCase`. They are provided in `PascalCase` in the example tables below.

## Main Config
| Field       	| Description                                                                	| Required                                        	| Default 	|
|-------------	|----------------------------------------------------------------------------	|-------------------------------------------------	|---------	|
| Mode        	| Deprecated: Mode to run the program in                                     	| No                                              	| Normal  	|
| Debug       	| Enable debug mode (Debug level logging and no destructive actions are run) 	| No                                              	| false   	|
| Verbose     	| Enable verbose logging                                                     	| No                                              	| false   	|
| AniDb       	| AniDb Config                                                               	| Only for commands that use the AniDb connection 	| N/A     	|
| Sources     	| List of directories to look for files                                      	| Yes                                             	| N/A     	|
| Destination 	| Destination configuration section                                          	| Yes                                             	| N/A     	|

## AniDb Config
| Field                     	| Description                                                                     	| Default 	|
|---------------------------	|---------------------------------------------------------------------------------	|---------	|
| Username                  	| Username to login to AniDb                                                      	| N/A     	|
| Password                  	| Password to login to AniDb                                                      	| N/A     	|
| MaxFileSearchRetries      	| Max number of attempts for searching for a file in AniDb                        	| 10      	|
| FileSearchCooldownMinutes 	| The time in minutes to wait between checking with AniDb again for a file's info 	| 300     	|

## Destination Config
| Field          	| Description                                                                                                                                  	| Required 	| Default 	|
|----------------	|----------------------------------------------------------------------------------------------------------------------------------------------	|----------	|---------	|
| NewFilePath    	| Path to move newly found files to                                                                                                            	| Yes      	| N/A     	|
| TvPath         	| Relative name of TV path                                                                                                                     	| Yes      	| N/A     	|
| MoviePath      	| Relative name of movie path                                                                                                                  	| Yes      	| N/A     	|
| Format         	| File path format for where to place and name the file inside either the tv or movie folder. For more info see the File Organization section. 	| Yes      	| N/A     	|
| Paths          	| Library paths to watch (not yet implemented)                                                                                                 	| No       	| N/A     	|
| FragmentSeries 	| When moving files, allow series to be fragmented (not yet implemented)                                                                       	| No       	| true    	|

## File Organization
To control the naming of files you can provide a custom format string such as  
`{animeRomaji}\{animeRomaji} - {episodeNumber} - {episodeEnglish...} [{subGroupShort}][{resolution}][{videoCodec}][{crc32}]`  
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
* `group` - Full release group name
* `groupShort` - Abbreviated release group name
* `resolution` - Video resolution for the file
* `videoCodec` - Video codec for the file
* `crc32` - CRC32 hash of the file in hex
* `ed2k` - eD2k hash of the file in hex
* `md5` - MD5 hash of the file in hex
* `sha1` - SHA-1 hash of the file in hex

#### Prefixes and Suffixes
You can also add conditionally rendered prefixes and suffixes to a variable by starting or ending a variable block with apostrophes. So as an example if you wanted the file version to be prefixed with v you could do a format string of `{episodeNumber}{'v'fileVersion}` to output `02v2` for the above example file assuming the file version is the second version. Since `{fileVersion}` is only emitted to the path when it is greater than 1 the same format string would render `02` for version one of the file.

#### Ellipsizing
Any variable can optionally be ellipsized to meet path limitations for certain file systems. Trailing the variable with `...` will cause the path to be ellipsized if needed to meet the OS defined path limitations. As an example, if for whatever reason the path above were limited to be two characters shorter it would output `Koukaku Kidoutai S.A.C. 2nd GIG\Koukaku Kidoutai S.A.C. 2nd GIG - 02 - Night C...[Hi10][1280x688][h264][D7083952]` instead to adhere to the path limits.  
As a quick note this is just set to 255. I need to look further into Linux/BSD/macOS path length limits, but for the time being 255 seems to be a safe assumption and satisfies Windows' extremely short path length.

#### Escape Sequences
If you want to use curly braces in your paths for whatever reason they can be escape them by using two of whichever curly brace you are trying to use.
