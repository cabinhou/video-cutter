using LibVLCSharp.Shared;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using VideoCutter.Wpf.ViewModels;

namespace VideoCutter.Wpf;

public partial class MainWindow : Window
{
    private static readonly MediaBrush DefaultVideoBorderBrush = new MediaSolidColorBrush(MediaColor.FromRgb(51, 51, 51));
    private static readonly MediaBrush ActiveVideoBorderBrush = new MediaSolidColorBrush(MediaColor.FromRgb(255, 209, 102));

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

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Focusable = true;
    }

    private async void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.mov;*.mkv;*.avi;*.flv;*.wmv|All Files|*.*"
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
            Filter = "MP4 Files|*.mp4|MKV Files|*.mkv|All Files|*.*",
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
        UpdateMarkerOverlay();
    }

    private void SetEndFromCurrent_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.EndTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
        UpdateMarkerOverlay();
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

        UpdateMarkerOverlay();
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
                UpdateMarkerOverlay();
                e.Handled = true;
                break;
            case Key.O:
                _viewModel.EndTimeText = GetCurrentPlaybackTime().ToString("hh\\:mm\\:ss");
                UpdateMarkerOverlay();
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
        UpdateMarkerOverlay();
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
        UpdateMarkerOverlay();
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

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.StartTimeText) or nameof(MainViewModel.EndTimeText))
        {
            UpdateMarkerOverlay();
        }
    }

    private void MarkerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateMarkerOverlay();
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

    private void UpdateMarkerOverlay()
    {
        if (!IsLoaded)
            return;

        var trackWidth = Math.Max(0, MarkerCanvas.ActualWidth);
        RangeTrack.Width = trackWidth;

        var duration = GetDuration();
        if (duration <= TimeSpan.Zero || trackWidth <= 0)
        {
            CollapseMarkerOverlay();
            VideoHostBorder.BorderBrush = DefaultVideoBorderBrush;
            return;
        }

        var hasStart = TimeSpan.TryParse(_viewModel.StartTimeText, out var startTime);
        var hasEnd = TimeSpan.TryParse(_viewModel.EndTimeText, out var endTime);

        if (hasStart)
        {
            startTime = ClampTime(startTime, duration);
            StartMarkerTextBlock.Text = startTime.ToString("hh\\:mm\\:ss");
            StartMarkerBadge.Visibility = Visibility.Visible;
            StartMarkerLine.Visibility = Visibility.Visible;
            Canvas.SetLeft(StartMarkerLine, GetMarkerLeft(startTime, duration, StartMarkerLine.Width, trackWidth));
        }
        else
        {
            StartMarkerBadge.Visibility = Visibility.Collapsed;
            StartMarkerLine.Visibility = Visibility.Collapsed;
        }

        if (hasEnd)
        {
            endTime = ClampTime(endTime, duration);
            EndMarkerTextBlock.Text = endTime.ToString("hh\\:mm\\:ss");
            EndMarkerBadge.Visibility = Visibility.Visible;
            EndMarkerLine.Visibility = Visibility.Visible;
            Canvas.SetLeft(EndMarkerLine, GetMarkerLeft(endTime, duration, EndMarkerLine.Width, trackWidth));
        }
        else
        {
            EndMarkerBadge.Visibility = Visibility.Collapsed;
            EndMarkerLine.Visibility = Visibility.Collapsed;
        }

        if (hasStart && hasEnd)
        {
            if (endTime < startTime)
            {
                (startTime, endTime) = (endTime, startTime);
                StartMarkerTextBlock.Text = startTime.ToString("hh\\:mm\\:ss");
                EndMarkerTextBlock.Text = endTime.ToString("hh\\:mm\\:ss");
                Canvas.SetLeft(StartMarkerLine, GetMarkerLeft(startTime, duration, StartMarkerLine.Width, trackWidth));
                Canvas.SetLeft(EndMarkerLine, GetMarkerLeft(endTime, duration, EndMarkerLine.Width, trackWidth));
            }

            var startX = GetMarkerCenter(startTime, duration, trackWidth);
            var endX = GetMarkerCenter(endTime, duration, trackWidth);
            Canvas.SetLeft(SelectedRangeBar, startX);
            SelectedRangeBar.Width = Math.Max(0, endX - startX);
            SelectedRangeBar.Visibility = Visibility.Visible;
        }
        else
        {
            SelectedRangeBar.Visibility = Visibility.Collapsed;
        }

        VideoHostBorder.BorderBrush = hasStart || hasEnd ? ActiveVideoBorderBrush : DefaultVideoBorderBrush;
    }

    private void CollapseMarkerOverlay()
    {
        StartMarkerBadge.Visibility = Visibility.Collapsed;
        EndMarkerBadge.Visibility = Visibility.Collapsed;
        StartMarkerLine.Visibility = Visibility.Collapsed;
        EndMarkerLine.Visibility = Visibility.Collapsed;
        SelectedRangeBar.Visibility = Visibility.Collapsed;
    }

    private static TimeSpan ClampTime(TimeSpan time, TimeSpan duration)
    {
        if (time < TimeSpan.Zero)
            return TimeSpan.Zero;
        if (time > duration)
            return duration;
        return time;
    }

    private static double GetMarkerCenter(TimeSpan time, TimeSpan duration, double trackWidth)
    {
        if (duration.TotalMilliseconds <= 0 || trackWidth <= 0)
            return 0;

        var ratio = time.TotalMilliseconds / duration.TotalMilliseconds;
        return Math.Max(0, Math.Min(trackWidth, ratio * trackWidth));
    }

    private static double GetMarkerLeft(TimeSpan time, TimeSpan duration, double markerWidth, double trackWidth)
    {
        var center = GetMarkerCenter(time, duration, trackWidth);
        return Math.Max(0, Math.Min(trackWidth - markerWidth, center - (markerWidth / 2)));
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _positionTimer.Stop();
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
