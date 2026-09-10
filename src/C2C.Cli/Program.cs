using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using C2C.Cli.Composition;

namespace C2C.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection()
            .AddCliServices()
            .BuildServiceProvider();

        var rootCommand = CliApplication.BuildRootCommand(services);
        return await rootCommand.Parse(args).InvokeAsync();
    }
}
