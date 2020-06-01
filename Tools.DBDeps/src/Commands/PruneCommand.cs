namespace Tools.DBDeps.Commands
{
  using McMaster.Extensions.CommandLineUtils;

  internal class PruneCommand : ICommand
  {
    private readonly DependencyManager dependencyManager;

    public PruneCommand(DependencyManager dependencyManager)
    {
      this.dependencyManager = dependencyManager;
    }

    public int OnExecute(CommandLineApplication application, IConsole console)
    {
      this.dependencyManager.PruneDependencies();
      return 0;
    }
  }
}