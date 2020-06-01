namespace Tools.DBDeps
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Tools.DBDeps.Commands;
  using Tools.DBDeps.Configuration;
  using Tools.DBDeps.Nuget;

  internal class DependencyManager
  {
    private const string dbDepsFolderName = "db_deps";

    private readonly NugetClient nugetClient;
    private readonly DBDepsConfigurationFile configurationFile;

    public DependencyManager(NugetClient nugetClient, DBDepsConfigurationFile configurationFile)
    {
      this.nugetClient = nugetClient;
      this.configurationFile = configurationFile;
    }

    public async Task AddDependency(string name, string version)
    {
      await this.InstallDependency(name, version);
      this.configurationFile.AddOrUpdateDependency(name, version);
      await this.configurationFile.SaveChanges();
    }

    public async Task RemoveDependency(string name)
    {
      this.configurationFile.RemoveDependency(name);
      await this.configurationFile.SaveChanges();
      this.PruneDependencies();
    }

    public async Task InstallDependency(string name, string version)
    {
      var dependencyPackagePath = await this.DownloadDependency(name, version);
      this.CopyDependencyFilesToDependenciesFolder(dependencyPackagePath, name, version);
    }

    public void PruneDependencies()
    {
      if (!Directory.Exists(this.GetDependenciesFolderPath()))
      {
        return;
      }

      var depsFolderInfo = new DirectoryInfo(this.GetDependenciesFolderPath());

      foreach (var dep in depsFolderInfo.GetDirectories())
      {
        if (!this.configurationFile.Dependencies.ContainsKey(dep.Name))
        {
          dep.Delete(true);
          continue;
        }

        var depVersion = this.configurationFile.Dependencies[dep.Name];

        foreach (var version in dep.GetDirectories()
          .Where(d => !d.Name.Equals(depVersion, StringComparison.InvariantCultureIgnoreCase)))
        {
          version.Delete(true);
        }
      }
    }

    private async Task<string> DownloadDependency(string name, string version)
    {
      var tempPackageFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      await this.nugetClient.DownloadAndExtractPackage(name, version, tempPackageFolder);
      return tempPackageFolder;
    }

    private void CopyDependencyFilesToDependenciesFolder(string dependencyPackagePath, string name, string version)
    {
      var sourceFolder = Path.Combine(dependencyPackagePath, @"content\externalDb");
      if (!Directory.Exists(sourceFolder))
      {
        var exceptionMessage =
          $@"Package ""{name}"" with version ""{version}"" does not contain standard DB files structure (packageRoot/content/externalDb).";
        throw new InvalidOperationException(exceptionMessage);
      }

      var destFolder = Path.Combine(this.GetDependenciesFolderPath(), name, version);
      new DirectoryInfo(sourceFolder).Copy(destFolder, false);
    }

    private string GetDependenciesFolderPath()
    {
      return Path.Combine(Directory.GetCurrentDirectory(), DependencyManager.dbDepsFolderName);
    }
  }
}