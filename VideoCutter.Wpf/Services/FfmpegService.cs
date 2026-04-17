using System.Diagnostics;
using VideoCutter.Wpf.Models;

namespace VideoCutter.Wpf.Services;

public sealed class FfmpegService
{
    public async Task RunCutAsync(
        string inputPath,
        string outputPath,
        string startTime,
        string endTime,
        CutMode mode,
        Action<string> onLog,
        Action<double>? onProgress = null)
    {
        var args = BuildArguments(inputPath, outputPath, startTime, endTime, mode);

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.Start();

        var stdoutTask = Task.Run(async () =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line)) onLog(line);
            }
        });

        var stderrTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    onLog(line);
                    if (line.Contains("time="))
                    {
                        onProgress?.Invoke(50);
                    }
                }
            }
        });

        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("ffmpeg 执行失败，请查看日志。\n\n" + args);
        }

        onProgress?.Invoke(100);
    }

    private static string BuildArguments(string inputPath, string outputPath, string startTime, string endTime, CutMode mode)
    {
        return mode switch
        {
            CutMode.FastStreamCopy =>
                $"-y -ss {startTime} -to {endTime} -i \"{inputPath}\" -c copy \"{outputPath}\"",

            CutMode.AccurateReencode =>
                $"-y -i \"{inputPath}\" -ss {startTime} -to {endTime} -c:v libx264 -preset veryfast -crf 18 -c:a aac -b:a 192k \"{outputPath}\"",

            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
