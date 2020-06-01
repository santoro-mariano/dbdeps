namespace Tools.DBDeps.Commands
{
  using System.Reflection;
  using McMaster.Extensions.CommandLineUtils;

  [Command("dotnet-dbdeps")]
  [VersionOptionFromMember("--version", MemberName = nameof(DbDepsCommand.GetVersion))]
  [Subcommand(typeof(ListCommand), typeof(InstallCommand), typeof(RemoveCommand), typeof(UpdateCommand),
    typeof(PruneCommand))]
  internal sealed class DbDepsCommand : ICommand
  {
    public int OnExecute(CommandLineApplication application, IConsole console)
    {
      application.ShowHelp();
      return 1;
    }

    private static string GetVersion()
      => typeof(DbDepsCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;
  }
}