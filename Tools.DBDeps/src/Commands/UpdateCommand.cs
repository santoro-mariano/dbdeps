namespace Tools.DBDeps.Commands
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using ConsoleTables;
  using McMaster.Extensions.CommandLineUtils;
  using MoreLinq;
  using Tools.DBDeps.Configuration;
  using Tools.DBDeps.Nuget;

  internal class UpdateCommand : IAsyncCommand
  {
    private readonly NugetClient nugetClient;
    private readonly DBDepsConfigurationFile configurationFile;
    private readonly DependencyManager dependencyManager;

    public UpdateCommand(NugetClient nugetClient, DBDepsConfigurationFile configurationFile,
      DependencyManager dependencyManager)
    {
      this.nugetClient = nugetClient;
      this.configurationFile = configurationFile;
      this.dependencyManager = dependencyManager;
    }

    [Option("-p|--include-prerelease", "Include prerelease packages.", CommandOptionType.NoValue)]
    public bool IncludePrerelease { get; set; }

    [Option("-l|--list", "Just list all packages that could be updated without update them.",
      CommandOptionType.NoValue)]
    public bool List { get; set; }

    public async Task OnExecuteAsync()
    {
      var updatedDependencies = new List<Tuple<string, string, string>>();

      foreach (var (depName, depVersion) in this.configurationFile.Dependencies.ToDictionary())
      {
        var latestVersion = await this.nugetClient.GetLatestVersion(depName, this.IncludePrerelease);
        if (depVersion.Equals(latestVersion, StringComparison.InvariantCultureIgnoreCase))
        {
          continue;
        }

        if (!this.List)
        {
          await this.dependencyManager.AddDependency(depName, latestVersion);
          this.dependencyManager.PruneDependencies();
        }

        updatedDependencies.Add(new Tuple<string, string, string>(depName, depVersion, latestVersion));
      }

      if (updatedDependencies.Count != 0)
      {
        dynamic formattedDependencies;

        if (this.List)
        {
          formattedDependencies = updatedDependencies.Select(t => new
          {
            Dependency = t.Item1,
            InstalledVersion = t.Item2,
            LatestVersion = t.Item3
          });
        }
        else
        {
          formattedDependencies = updatedDependencies.Select(t => new
          {
            Dependency = t.Item1,
            PreviousVersion = t.Item2,
            InstalledVersion = t.Item3
          });
        }

        ConsoleTable.From(formattedDependencies).Write();
      }

      Console.WriteLine("All dependencies are up to date.");
    }
  }
}