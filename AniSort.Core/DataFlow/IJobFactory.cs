using System.Collections.Generic;
using AniSort.Core.Data;

namespace AniSort.Core.DataFlow;

public interface IJobFactory
{
    IEnumerable<Job> CreateSortDirectoryJobs(string name, string path);
    
    Job CreateSortFileJob(string name, string path);

    IEnumerable<Job> CreateHashDirectoryJobs(string name, string path);

    Job CreateHashFileJob(string name, string path);
}
