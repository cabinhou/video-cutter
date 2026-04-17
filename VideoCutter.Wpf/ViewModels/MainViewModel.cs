using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using VideoCutter.Wpf.Models;
using VideoCutter.Wpf.Services;

namespace VideoCutter.Wpf.ViewModels;

public sealed class MainViewModel : BindableBase
{
    private readonly FfprobeService _ffprobeService = new();
    private readonly FfmpegService _ffmpegService = new();
    private readonly StringBuilder _logBuilder = new();

    private string _inputPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _startTimeText = "00:00:00";
    private string _endTimeText = "00:00:10";
    private string _mediaInfoText = "尚未选择视频文件。";
    private string _logText = string.Empty;
    private double _progress;
    private string _statusText = "未开始处理";
    private string _progressText = "0%";
    private CutMode _selectedMode = CutMode.FastStreamCopy;
    private MediaInfo? _currentMediaInfo;

    public MainViewModel()
    {
        Modes = new ObservableCollection<CutMode>
        {
            CutMode.FastStreamCopy,
            CutMode.AccurateReencode
        };
    }

    public ObservableCollection<CutMode> Modes { get; }

    public string InputPath
    {
        get => _inputPath;
        set => SetProperty(ref _inputPath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        set => SetProperty(ref _endTimeText, value);
    }

    public string MediaInfoText
    {
        get => _mediaInfoText;
        set => SetProperty(ref _mediaInfoText, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public CutMode SelectedMode
    {
        get => _selectedMode;
        set => SetProperty(ref _selectedMode, value);
    }

    public string SuggestedOutputFileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(InputPath)) return "output.mp4";
            var dir = Path.GetDirectoryName(InputPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(InputPath);
            return Path.Combine(dir, name + "_cut.mp4");
        }
    }

    public async Task LoadInputAsync(string filePath)
    {
        InputPath = filePath;
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            OutputPath = SuggestedOutputFileName;
        }

        AppendLog($"加载文件：{filePath}");
        StatusText = "已加载视频";
        Progress = 0;
        ProgressText = "0%";
        _currentMediaInfo = await _ffprobeService.ProbeAsync(filePath);
        MediaInfoText = $"时长：{TimeSpan.FromSeconds(_currentMediaInfo.DurationSeconds):hh\\:mm\\:ss} | 路径：{filePath}";
        EndTimeText = TimeSpan.FromSeconds(Math.Min(_currentMediaInfo.DurationSeconds, 10)).ToString("hh\\:mm\\:ss");
    }

    public async Task CutAsync()
    {
        Validate();

        Progress = 0;
        ProgressText = "0%";
        StatusText = "正在裁剪...";
        AppendLog("开始执行裁剪...");

        await _ffmpegService.RunCutAsync(
            InputPath,
            OutputPath,
            StartTimeText,
            EndTimeText,
            SelectedMode,
            AppendLog,
            p =>
            {
                Progress = p;
                ProgressText = $"{p:0}%";
            });

        StatusText = "裁剪完成";
        Progress = 100;
        ProgressText = "100%";
        AppendLog("裁剪完成。输出文件：" + OutputPath);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
            throw new InvalidOperationException("请先选择有效的视频文件。");

        if (string.IsNullOrWhiteSpace(OutputPath))
            throw new InvalidOperationException("请选择输出文件路径。");

        if (!TimeSpan.TryParse(StartTimeText, out _))
            throw new InvalidOperationException("开始时间格式无效，应为 hh:mm:ss。");

        if (!TimeSpan.TryParse(EndTimeText, out _))
            throw new InvalidOperationException("结束时间格式无效，应为 hh:mm:ss。");
    }

    private void AppendLog(string line)
    {
        _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        LogText = _logBuilder.ToString();
    }
}
