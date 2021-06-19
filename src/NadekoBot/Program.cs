using NadekoBot;
using NadekoBot.Core.Services;
using Serilog;

var pid = System.Environment.ProcessId;

var shardId = 0;
if (args.Length == 1)
    int.TryParse(args[0], out shardId);

LogSetup.SetupLogger(shardId);
Log.Information($"Pid: {pid}");

await new Bot(shardId).RunAndBlockAsync();