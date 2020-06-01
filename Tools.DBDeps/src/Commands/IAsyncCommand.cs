namespace Tools.DBDeps.Commands
{
  using System.Threading.Tasks;

  internal interface IAsyncCommand
  {
    Task OnExecuteAsync();
  }
}