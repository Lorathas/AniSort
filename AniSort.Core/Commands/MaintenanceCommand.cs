using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AniSort.Core.MaintenanceTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

public class MaintenanceCommand : ICommand
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MaintenanceCommand> logger;

    public MaintenanceCommand(IServiceProvider serviceProvider, ILogger<MaintenanceCommand> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync()
    {
        foreach (var maintenanceTaskType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => typeof(IMaintenanceTask).IsAssignableFrom(t) && !t.IsInterface))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            
            var instance = scope.ServiceProvider.GetService(maintenanceTaskType);

            if (instance is not IMaintenanceTask maintenanceTask)
            {
                logger.LogWarning("Maintenance Task of type {MaintenanceTaskType} is not injectable", maintenanceTaskType.FullName);
                continue;
            }

            logger.LogDebug("Running maintenance task: {MaintenanceTask}", maintenanceTask.UserFacingName);
            await maintenanceTask.RunAsync();
            logger.LogInformation("Ran maintenance task: {MaintenanceTask}", maintenanceTask.UserFacingName);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> CommandName => new [] {"maint", "maintenance"};
}
