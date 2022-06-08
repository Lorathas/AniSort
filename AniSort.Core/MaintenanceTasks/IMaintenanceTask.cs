using System.Threading.Tasks;

namespace AniSort.Core.MaintenanceTasks;

public interface IMaintenanceTask
{
    Task RunAsync();
    
    string Description { get; }
    
    string CommandName { get; }
}
