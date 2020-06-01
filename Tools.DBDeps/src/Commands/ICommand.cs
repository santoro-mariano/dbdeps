namespace Tools.DBDeps.Commands
{
  using McMaster.Extensions.CommandLineUtils;

  internal interface ICommand
  {
    int OnExecute(CommandLineApplication application, IConsole console);
  }
}