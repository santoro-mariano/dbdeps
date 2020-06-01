namespace Tools.DBDeps.Commands
{
  using System.ComponentModel.DataAnnotations;
  using System.Threading.Tasks;
  using McMaster.Extensions.CommandLineUtils;

  internal sealed class RemoveCommand : IAsyncCommand
  {
    private readonly DependencyManager dependencyManager;

    public RemoveCommand(DependencyManager dependencyManager)
    {
      this.dependencyManager = dependencyManager;
    }

    [Required]
    [Argument(0, "packageId", Description = "Nuget Package Id")]
    public string PackageId { get; set; }

    public async Task OnExecuteAsync()
    {
      await this.dependencyManager.RemoveDependency(this.PackageId);
    }
  }
}