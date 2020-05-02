# AniSort
AniSort is a command line program for organizing files that uses the AniDB UDP API for fetching info for a file.

The solution is split into the three following projects.  
## AniSort
The console program itself that uses functionality from the AniSort.Core and AniDbSharp projects to provide a console program for sorting anime.

## AniSort.Core
Core functionality of the console program is put here. Going forwards I would like to add a WPF project that provides similar functionality to the console program so any functionality that would be useful in both the console and the GUI will be stored here.

## AniDbSharp 
This project contains code for interfacing with the AniDB UDP API. Eventually I'd like to split this off into it's own separate repository and NuGet package, but until the functionality is more mature it will just live here.