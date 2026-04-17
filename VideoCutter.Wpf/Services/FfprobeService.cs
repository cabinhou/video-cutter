using System.Diagnostics;
using System.Text.Json;
using VideoCutter.Wpf.Models;

namespace VideoCutter.Wpf.Services;

public sealed class FfprobeService
{
    public async Task<MediaInfo> ProbeAsync(string inputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffprobe 执行失败：{error}");
        }

        using var doc = JsonDocument.Parse(output);
        var format = doc.RootElement.GetProperty("format");
        var durationText = format.GetProperty("duration").GetString();
        _ = double.TryParse(durationText, out var durationSeconds);

        return new MediaInfo
        {
            DurationSeconds = durationSeconds,
            RawJson = output
        };
    }
}
