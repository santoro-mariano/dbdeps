namespace Tools.DBDeps
{
  using System;
  using System.IO;
  using System.Threading.Tasks;
  using McMaster.Extensions.CommandLineUtils;
  using Microsoft.Extensions.DependencyInjection;
  using Tools.DBDeps.Commands;
  using Tools.DBDeps.Configuration;
  using Tools.DBDeps.Nuget;

  class Program
  {
    static int Main(string[] args)
    {
#if DEBUG
      while (true)
      {
        var input = Prompt.GetString("> ");
        var inputSplit = input?.Split(' ') ?? new string[1];

        if (inputSplit[0] == "clear")
        {
          Console.Clear();
          continue;
        }

        if (inputSplit[0] == "exit")
        {
          // Exit out
          return 0;
        }

        Program.RunApp(inputSplit).GetAwaiter().GetResult();
      }
#else
            return Program.RunApp(args).GetAwaiter().GetResult();
#endif
    }

    private static async Task<int> RunApp(string[] args)
    {
      try
      {
        var services = new ServiceCollection();
        Program.ConfigureServices(services);
        var app = new CommandLineApplication<DbDepsCommand>();
        app.Conventions.UseDefaultConventions().UseConstructorInjection(services.BuildServiceProvider());
        return await app.ExecuteAsync(args);
      }
      catch (Exception e)
      {
        var f = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = f;
        return 1;
      }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
      services.AddSingleton<NugetClient>();
      services.AddSingleton<DependencyManager>();
      services.AddSingleton(x =>
      {
        var confFilePath = Path.Combine(Directory.GetCurrentDirectory(), "dbDeps.json");
        var conf = new DBDepsConfigurationFile(confFilePath);
        return conf;
      });
    }
  }
}