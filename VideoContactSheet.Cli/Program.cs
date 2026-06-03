namespace VideoContactSheet.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var cli = new CliOptions();
        var rootCommand = cli.BuildRootCommand();

        rootCommand.SetAction((parseResult, ct) => SheetRunner.RunAsync(cli.Bind(parseResult), ct));

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
