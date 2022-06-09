using System;
using System.Collections.Generic;
using System.Linq;
using AniSort.Core.Commands;
using AniSort.Core.MaintenanceTasks;

namespace AniSort.Core.Helpers;

public class AssemblyHelpers
{
    private static Type[] commandTypes;
    private static Type[] maintenanceTaskTypes;
    
    public static IEnumerable<Type> CommandTypes => commandTypes ??= AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface).ToArray();
    
    public static IEnumerable<Type> MaintenanceTaskTypes => maintenanceTaskTypes ??= AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => typeof(IMaintenanceTask).IsAssignableFrom(t) && !t.IsInterface).ToArray();
}
