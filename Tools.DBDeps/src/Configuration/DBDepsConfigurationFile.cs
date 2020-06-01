namespace Tools.DBDeps.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using System.Threading.Tasks;

  internal class DBDepsConfigurationFile
  {
    private readonly string configurationFilePath;
    private readonly Dictionary<string, string> dependencies = new Dictionary<string, string>();

    public DBDepsConfigurationFile(string configurationFilePath)
    {
      this.configurationFilePath = configurationFilePath;
      this.Load();
    }

    [JsonPropertyName("dependencies")] public IReadOnlyDictionary<string, string> Dependencies => this.dependencies;

    public void AddOrUpdateDependency(string name, string version)
    {
      var normalizedName = name.ToLowerInvariant();
      if (this.dependencies.ContainsKey(normalizedName))
      {
        this.dependencies[normalizedName] = version.ToLowerInvariant();
      }
      else
      {
        this.dependencies.Add(name.ToLowerInvariant(), version.ToLowerInvariant());
      }
    }

    public void RemoveDependency(string name)
    {
      var normalizedName = name.ToLowerInvariant();
      if (!this.dependencies.ContainsKey(normalizedName))
      {
        throw new InvalidOperationException("Dependency doesn't exist.");
      }

      this.dependencies.Remove(normalizedName);
    }

    public async Task SaveChanges()
    {
      await using var docStream = File.Create(this.configurationFilePath);
      await JsonSerializer.SerializeAsync(docStream, this);
      docStream.Close();
    }

    private void Load()
    {
      if (string.IsNullOrWhiteSpace(this.configurationFilePath))
      {
        throw new InvalidOperationException("Configuration file path is invalid.");
      }

      if (!File.Exists(this.configurationFilePath))
      {
        return;
      }

      this.dependencies.Clear();
      using var docStream = File.OpenRead(this.configurationFilePath);
      var doc = JsonDocument.Parse(docStream);
      var deps = doc.RootElement.GetProperty("dependencies");
      foreach (var dep in deps.EnumerateObject())
      {
        this.dependencies.Add(dep.Name, dep.Value.GetString());
      }
    }
  }
}