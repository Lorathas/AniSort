using System.Text;
using AniSort.Server.Generators.Models;

namespace AniSort.Server.Generators;

public static class SourceGenerationHelper
{
    public const string HubAttribute = @"namespace AniSort.Server.Generators;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class HubAttribute : System.Attribute
{
}";

    public static string GenerateHubServiceClass(HubServiceToGenerate hubToGenerate)
    {
        var builder = new StringBuilder();

        builder.Append("namespace ");
        builder.Append(hubToGenerate.Namespace);
        builder.Append(".Services;");
        builder.AppendLine();

        builder.AppendLine();

        builder.Append("using AniSort.Server.Hubs;");
        builder.AppendLine();
        
        builder.AppendLine();

        builder.Append("public class ");
        builder.Append(hubToGenerate.Name);
        builder.Append("Service : BackgroundService");
        builder.AppendLine();

        builder.Append('{');
        builder.AppendLine();

        builder.Append("    private readonly ");
        builder.Append(hubToGenerate.InterfaceName);
        builder.Append(" hub;");
        builder.AppendLine();

        builder.AppendLine();

        builder.Append("    public ");
        builder.Append(hubToGenerate.Name);
        builder.Append("Service(");
        builder.Append(hubToGenerate.InterfaceName);
        builder.Append(" hub)");
        builder.AppendLine();

        builder.Append("    {");
        builder.AppendLine();

        builder.Append("        this.hub = hub;");
        builder.AppendLine();

        builder.Append("    }");
        builder.AppendLine();
        builder.AppendLine();

        builder.Append("    ///<inheritdoc/>");
        builder.AppendLine();

        builder.Append("    protected override async Task ExecuteAsync(CancellationToken stoppingToken)");
        builder.AppendLine();

        builder.Append("    {");
        builder.AppendLine();

        builder.Append("        await hub.RunAsync(stoppingToken);");
        builder.AppendLine();

        builder.Append("    }");
        builder.AppendLine();

        builder.Append('}');


        return builder.ToString();
    }

    public static string GenerateHubServiceRegistrationClass(List<HubServiceToGenerate> hubServiceToGenerates)
    {
        var builder = new StringBuilder();

        builder.Append("namespace AniSort.Server.Generators;");
        builder.AppendLine();

        builder.AppendLine();

        var namespacesIncluded = new HashSet<string>();
        
        foreach (var hubToGenerate in hubServiceToGenerates.Where(hubToGenerate => !namespacesIncluded.Contains(hubToGenerate.Namespace)))
        {
            builder.Append("using ");
            builder.Append(hubToGenerate.Namespace);
            builder.Append(".Services;");
            builder.AppendLine();

            namespacesIncluded.Add(hubToGenerate.Namespace);
        }
        builder.AppendLine();

        builder.Append("public static class HubServiceRegistration");
        builder.AppendLine();

        builder.Append("{");
        builder.AppendLine();

        builder.Append("    public static void RegisterServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        builder.AppendLine();

        builder.Append("    {");
        builder.AppendLine();

        foreach (var hubServiceToGenerate in hubServiceToGenerates)
        {
            builder.Append($"       services.AddHostedService<{hubServiceToGenerate.Name}Service>();");
            builder.AppendLine();
        }

        builder.Append("    }");
        builder.AppendLine();

        builder.Append("}");
        

        return builder.ToString();
    }
}
