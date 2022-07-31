#nullable disable
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Wiz.Common;

namespace WizBot.Services;

public class YtdlOperation
{
    private readonly string _baseArgString;
    private readonly bool _isYtDlp;

    public YtdlOperation(string baseArgString, bool isYtDlp = false)
    {
        _baseArgString = baseArgString;
        _isYtDlp = isYtDlp;
    }

    private Process CreateProcess(string[] args)
    {
        var newArgs = args.Map(arg => (object)arg.Replace("\"", ""));
        return new()
        {
            StartInfo = new()
            {
                FileName = _isYtDlp ? "yt-dlp" : "youtube-dl",
                Arguments = string.Format(_baseArgString, newArgs),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            }
        };
    }

    public async Task<string> GetDataAsync(params string[] args)
    {
        try
        {
            using var process = CreateProcess(args);

            Log.Debug("Executing {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Start();

            var str = await process.StandardOutput.ReadToEndAsync();
            var err = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(err))
                Log.Warning("YTDL warning: {YtdlWarning}", err);

            return str;
        }
        catch (Win32Exception)
        {
            Log.Error("youtube-dl is likely not installed. " + "Please install it before running the command again");
            return default;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception running youtube-dl: {ErrorMessage}", ex.Message);
            return default;
        }
    }

    public async IAsyncEnumerable<string> EnumerateDataAsync(params string[] args)
    {
        using var process = CreateProcess(args);

        Log.Debug("Executing {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
        process.Start();

        string line;
        while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
            yield return line;
    }
}