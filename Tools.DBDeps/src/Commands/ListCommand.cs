namespace Tools.DBDeps.Commands
{
  using System.Linq;
  using System.Threading.Tasks;
  using ConsoleTables;
  using McMaster.Extensions.CommandLineUtils;
  using Tools.DBDeps.Configuration;
  using Tools.DBDeps.Nuget;

  internal sealed class ListCommand : IAsyncCommand
  {
    private readonly NugetClient nugetClient;
    private readonly DBDepsConfigurationFile configurationFile;

    public ListCommand(NugetClient nugetClient, DBDepsConfigurationFile configurationFile)
    {
      this.nugetClient = nugetClient;
      this.configurationFile = configurationFile;
    }

    [Argument(0, "query", Description = "The search terms used to find packages")]
    public string Query { get; set; }

    [Option("-p|--include-prerelease", "Include prerelease packages", CommandOptionType.NoValue)]
    public bool IncludePrerelease { get; set; }

    [Option("-a|--all-versions", "Include prerelease packages", CommandOptionType.NoValue)]
    public bool AllVersions { get; set; }

    public async Task OnExecuteAsync()
    {
      var pkgIds = string.IsNullOrWhiteSpace(this.Query)
        ? this.configurationFile.Dependencies.Keys.ToArray()
        : new[] {this.Query};

      var packages = await this.nugetClient.GetNugetPackages(pkgIds, this.IncludePrerelease, this.AllVersions);
      var formattedPackages = packages.Select(p =>
      {
        var installedVersion = this.configurationFile.Dependencies.ContainsKey(p.Id.ToLowerInvariant())
          ? this.configurationFile.Dependencies[p.Id.ToLowerInvariant()]
          : null;
        return new {Name = p.Id, LatestVersion = p.Version, InstalledVersion = installedVersion};
      });
      ConsoleTable.From(formattedPackages).Write();
    }
  }
}