using System.Threading.Tasks;

namespace AniSort.Core.MaintenanceTasks;

public interface IMaintenanceTask
{
    Task RunAsync();
    
    string UserFacingName { get; }
}
