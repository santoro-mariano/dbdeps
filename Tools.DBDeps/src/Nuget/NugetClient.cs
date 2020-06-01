namespace Tools.DBDeps.Nuget
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Compression;
  using System.Linq;
  using System.Net.Http;
  using System.Threading.Tasks;
  using MoreLinq;
  using Simple.OData.Client;

  internal class NugetClient
  {
    //TODO: Update client to V3 - https://api.nuget.org/v3/index.json
    private const string NugetPackageServerUrl = "https://www.nuget.org/api/v2/";

    public async Task<IEnumerable<NugetPackage>> GetNugetPackages(string[] searchKeywords, bool includePreRelease,
      bool allVersions)
    {
      if (searchKeywords == null)
      {
        throw new ArgumentNullException(nameof(searchKeywords));
      }

      //var annotations = new ODataFeedAnnotations();
      var result = await this.GetNugetClient()
        .Filter(this.GetFilter(searchKeywords, null, includePreRelease))
        .OrderBy(p => p.Id)
        .ThenByDescending(p => p.Version)
        .FindEntriesAsync(); //annotations

      return allVersions ? result : result.DistinctBy(x => x.Id);
    }

    public async Task DownloadAndExtractPackage(string packageId, string version, string destinationPath)
    {
      var packageFilePath = await this.DownloadPackage(packageId, version);
      this.ExtractPackage(packageFilePath, destinationPath);
    }

    public async Task<string> DownloadPackage(string packageId, string version)
    {
      if (string.IsNullOrWhiteSpace(packageId))
      {
        throw new ArgumentNullException(nameof(packageId));
      }

      if (string.IsNullOrWhiteSpace(version))
      {
        throw new ArgumentNullException(nameof(version));
      }

      var commandText = await this.GetNugetClient().Key(packageId, version)
        .Action("Download").GetCommandTextAsync();

      using var httpClient = new HttpClient();
      var package = await httpClient.GetByteArrayAsync($"{NugetClient.NugetPackageServerUrl}/{commandText}");
      var packageFilePath = Path.GetTempFileName();
      await using var fileStream = File.OpenWrite(packageFilePath);
      await fileStream.WriteAsync(new ReadOnlyMemory<byte>(package));
      await fileStream.FlushAsync();
      fileStream.Close();

      return packageFilePath;
    }

    public void ExtractPackage(string packagePath, string destinationPath)
    {
      ZipFile.ExtractToDirectory(packagePath, destinationPath);
    }

    public async Task<string> GetLatestVersion(string packageId, bool includePreRelease)
    {
      var query = this.GetNugetClient().Key(packageId, "");

      if (!includePreRelease)
      {
        query = query.Filter(this.GetExcludePreReleaseFilter());
      }

      query = query.OrderByDescending(p => p.Version)
        .Select(p => p.Version)
        .Top(1);

      return (await query.FindEntryAsync()).Version;
    }

    public async Task<bool> PackageExists(string packageId, string version = null)
    {
      var count = await this.GetNugetClient()
        .Filter(this.GetFilter(packageId, version))
        .Count()
        .FindScalarAsync<int>();
      return count > 0;
    }

    private IBoundClient<NugetPackage> GetNugetClient()
    {
      var client = new ODataClient(NugetClient.NugetPackageServerUrl);
      return client.For<NugetPackage>("Packages");
    }

    private string GetFilter(string packageId, string version = null, bool includePreRelease = true)
    {
      return this.GetFilter(new[] {packageId}, version, includePreRelease);
    }

    private string GetFilter(IEnumerable<string> packageIds, string version = null, bool includePreRelease = true)
    {
      var filter = string.Join(" or ", packageIds.Select(this.GetPackageIdFilter));

      if (!string.IsNullOrWhiteSpace(version))
      {
        filter += $" and {this.GetVersionFilter(version)}";
      }

      if (!includePreRelease)
      {
        filter += $" and {this.GetExcludePreReleaseFilter()}";
      }

      return filter;
    }

    private string GetPackageIdFilter(string packageId)
    {
      return $"substringof('{packageId}', tolower(Id))";
    }

    private string GetVersionFilter(string version)
    {
      return $"Version eq '{version}'";
    }

    private string GetExcludePreReleaseFilter()
    {
      return "IsPrerelease eq false";
    }
  }
}