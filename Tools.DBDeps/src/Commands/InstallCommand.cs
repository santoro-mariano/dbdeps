namespace Tools.DBDeps.Commands
{
  using System;
  using System.IO;
  using System.Threading.Tasks;
  using McMaster.Extensions.CommandLineUtils;
  using Tools.DBDeps.Configuration;
  using Tools.DBDeps.Nuget;

  internal sealed class InstallCommand : IAsyncCommand
  {
    private readonly DependencyManager dependencyManager;
    private readonly NugetClient nugetClient;
    private readonly DBDepsConfigurationFile configurationFile;

    public InstallCommand(DependencyManager dependencyManager,
      NugetClient nugetClient,
      DBDepsConfigurationFile configurationFile)
    {
      this.dependencyManager = dependencyManager;
      this.nugetClient = nugetClient;
      this.configurationFile = configurationFile;
    }

    [Argument(0, "packageId", Description = "Nuget Package Id")]
    public string PackageId { get; set; }

    [Option("-v|--version", "Specify version to install", CommandOptionType.SingleValue)]
    public string Version { get; set; }

    public async Task OnExecuteAsync()
    {
      if (string.IsNullOrWhiteSpace(this.PackageId))
      {
        foreach (var (depName, depVersion) in this.configurationFile.Dependencies)
        {
          await this.dependencyManager.InstallDependency(depName, depVersion);
        }
      }
      else
      {
        var version = string.IsNullOrWhiteSpace(this.Version)
          ? await this.nugetClient.GetLatestVersion(this.PackageId, true)
          : this.Version;

        if (!string.IsNullOrWhiteSpace(version) && await this.nugetClient.PackageExists(this.PackageId, version))
        {
          await this.dependencyManager.AddDependency(this.PackageId, version);
        }
        else
        {
          throw new InvalidOperationException(
            $@"Package ""{this.PackageId}"" [version: {this.Version}] could not be found.");
        }
      }

      this.dependencyManager.PruneDependencies();
    }
  }

  internal static class DirectoryExtensions
  {
    public static void Copy(this DirectoryInfo sourceDirectory, string destPath, bool includeRootFolder = true)
    {
      var destDirectory = includeRootFolder ? Path.Combine(destPath, sourceDirectory.Name) : destPath;

      if (!sourceDirectory.Exists)
      {
        throw new DirectoryNotFoundException(
          "Source directory does not exist or could not be found: "
          + sourceDirectory);
      }

      if (!Directory.Exists(destDirectory))
      {
        Directory.CreateDirectory(destDirectory);
      }

      var files = sourceDirectory.GetFiles();
      foreach (var file in files)
      {
        file.CopyTo(Path.Combine(destDirectory, file.Name), true);
      }

      var dirs = sourceDirectory.GetDirectories();
      foreach (var subDir in dirs)
      {
        Copy(new DirectoryInfo(subDir.FullName),
          includeRootFolder ? destDirectory : Path.Combine(destDirectory, subDir.Name),
          includeRootFolder);
      }
    }
  }
}