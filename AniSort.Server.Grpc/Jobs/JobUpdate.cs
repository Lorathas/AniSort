namespace AniSort.Server.JobManager;

public enum JobUpdate
{
    Created = 0,
    Started = 1,
    Progress = 2,
    Completed = 3,
    Failed = 4,
}