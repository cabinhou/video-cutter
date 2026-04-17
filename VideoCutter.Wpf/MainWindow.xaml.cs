using LibVLCSharp.Shared;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VideoCutter.Wpf.ViewModels;

namespace VideoCutter.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherTimer _positionTimer;
    private bool _isDraggingSlider;
    private bool _ignoreSliderChange;

    public MainWindow()
    {
        Core.Initialize();

        InitializeComponent();
        DataContext = _viewModel;

        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _mediaPlayer;

        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _positionTimer.Tick += PositionTimer_Tick;
        _positionTimer.Start();

        Closed += MainWindow_Closed;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Focusable = true;
    }

    private async void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.mov;*.mkv;*.avi;*.flv;*.wmv|所有文件|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.LoadInputAsync(dialog.FileName);
            LoadMedia(dialog.FileName);
            Focus();
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "MP4 文件|*.mp4|MKV 文件|*.mkv|所有文件|*.*",
            FileName = _viewModel.SuggestedOutputFileName
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.OutputPath = dialog.FileName;
        }
    }

    private async void Cut_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.CutAsync();
            MessageBox.Show(this, "裁剪完成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = "裁剪失败";
            MessageBox.Show(this, ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        PlayMedia();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        PauseMedia();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopMedia();
    }

    private void Back5Seconds_Click(object sender, RoutedEventArgs e)
    {
        SeekBySeconds(-5);
    }

    private void Forward5Seconds_Click(object sender, RoutedEventArgs e)
    {
        SeekBySeconds(5);
    }

    private void SetStartFromCurrent_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StartTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
    }

    private void SetEndFromCurrent_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.EndTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
    }

    private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = true;
    }

    private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_ignoreSliderChange)
            return;

        var duration = GetDuration();
        var position = (float)(PositionSlider.Value / PositionSlider.Maximum);
        var preview = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * position);
        SeekPreviewTextBlock.Text = preview.ToString("hh\\:mm\\:ss");

        if (_isDraggingSlider)
        {
            UpdateDisplayedTime(preview, duration);
        }
    }

    private void PositionSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_mediaPlayer.Media is null)
            return;

        _isDraggingSlider = false;
        var position = (float)(PositionSlider.Value / PositionSlider.Maximum);
        _mediaPlayer.Position = position;
        SeekPreviewTextBlock.Text = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaPlayer.Media is null)
            return;

        var duration = GetDuration();
        var current = GetCurrentPlaybackTime();
        UpdateDisplayedTime(current, duration);
        SeekPreviewTextBlock.Text = current.ToString("hh\\:mm\\:ss");

        if (!_isDraggingSlider)
        {
            SetSliderFromPosition(_mediaPlayer.Position);
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                TogglePlayPause();
                e.Handled = true;
                break;
            case Key.Left:
                SeekBySeconds(-5);
                e.Handled = true;
                break;
            case Key.Right:
                SeekBySeconds(5);
                e.Handled = true;
                break;
            case Key.I:
                _viewModel.StartTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
                e.Handled = true;
                break;
            case Key.O:
                _viewModel.EndTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
                e.Handled = true;
                break;
        }
    }

    private void LoadMedia(string filePath)
    {
        _mediaPlayer.Stop();
        using var media = new Media(_libVlc, new Uri(filePath));
        _mediaPlayer.Media = media;
        _mediaPlayer.Play();
        _mediaPlayer.Pause();
        _viewModel.StatusText = "视频已就绪，可播放和定位";
    }

    private void PlayMedia()
    {
        if (_mediaPlayer.Media is not null)
        {
            _mediaPlayer.Play();
            _viewModel.StatusText = "正在播放";
        }
    }

    private void PauseMedia()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            _viewModel.StatusText = "已暂停";
        }
    }

    private void StopMedia()
    {
        _mediaPlayer.Stop();
        UpdateDisplayedTime(TimeSpan.Zero, GetDuration());
        SetSliderFromPosition(0f);
        SeekPreviewTextBlock.Text = "00:00:00";
        _viewModel.StatusText = "已停止";
    }

    private void TogglePlayPause()
    {
        if (_mediaPlayer.Media is null)
            return;

        if (_mediaPlayer.IsPlaying)
        {
            PauseMedia();
        }
        else
        {
            PlayMedia();
        }
    }

    private void SeekBySeconds(int seconds)
    {
        if (_mediaPlayer.Media is null)
            return;

        var duration = GetDuration();
        var current = GetCurrentPlaybackTime();
        var target = current.Add(TimeSpan.FromSeconds(seconds));

        if (target < TimeSpan.Zero)
            target = TimeSpan.Zero;
        if (duration > TimeSpan.Zero && target > duration)
            target = duration;

        if (duration.TotalMilliseconds > 0)
        {
            _mediaPlayer.Position = (float)(target.TotalMilliseconds / duration.TotalMilliseconds);
            UpdateDisplayedTime(target, duration);
            SeekPreviewTextBlock.Text = target.ToString("hh\\:mm\\:ss");
            SetSliderFromPosition(_mediaPlayer.Position);
            _viewModel.StatusText = $"已定位到 {target:hh\\:mm\\:ss}";
        }
    }

    private TimeSpan GetDuration()
    {
        return _mediaPlayer.Length > 0
            ? TimeSpan.FromMilliseconds(_mediaPlayer.Length)
            : TimeSpan.Zero;
    }

    private TimeSpan GetCurrentPlaybackTime()
    {
        return _mediaPlayer.Time > 0
            ? TimeSpan.FromMilliseconds(_mediaPlayer.Time)
            : TimeSpan.Zero;
    }

    private void UpdateDisplayedTime(TimeSpan current, TimeSpan duration)
    {
        CurrentTimeTextBlock.Text = current.ToString("hh\\:mm\\:ss");
        DurationTextBlock.Text = duration.ToString("hh\\:mm\\:ss");
    }

    private void SetSliderFromPosition(float position)
    {
        _ignoreSliderChange = true;
        PositionSlider.Value = Math.Max(0, Math.Min(PositionSlider.Maximum, position * PositionSlider.Maximum));
        _ignoreSliderChange = false;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _positionTimer.Stop();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
