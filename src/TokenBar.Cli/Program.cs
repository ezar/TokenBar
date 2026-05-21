using TokenBar.Cli;

var exitCode = await CliApplication.RunAsync(args, Console.Out, CancellationToken.None);
return exitCode;
