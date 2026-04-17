# video-cutter-wpf

A local desktop video trimming tool built with `C#`, `WPF`, `FFmpeg`, and `LibVLCSharp`.

## Overview

This repository contains a Windows desktop MVP for basic video preview and clipping.

Current capabilities:

- Open a local video file
- Preview video inside the app with `LibVLCSharp`
- Play, pause, stop, and seek by 5 seconds
- Drag the timeline slider for rough positioning
- Set clip start and end timestamps from the current playback position
- Read media duration with `ffprobe`
- Export a clip with `ffmpeg`
- Choose between fast stream copy and accurate re-encode modes
- Show processing status, progress, and FFmpeg logs

## Stack

- UI: `WPF`
- Language: `C#`
- Video preview: `LibVLCSharp.WPF`
- Media inspection: `ffprobe`
- Video cutting: `ffmpeg`
- Target framework: `.NET 8`

## Repository Layout

```text
video-cutter-wpf/
|-- VideoCutter.Wpf.sln
|-- README.md
|-- .gitignore
|-- VideoCutter.Wpf/
    |-- App.xaml
    |-- MainWindow.xaml
    |-- MainWindow.xaml.cs
    |-- Models/
    |-- Services/
    |-- ViewModels/
    |-- VideoCutter.Wpf.csproj
```

## Prerequisites

Before running the app, make sure you have:

1. Windows
2. `.NET 8 SDK`
3. `ffmpeg` and `ffprobe` installed and available in `PATH`

NuGet dependencies used by the project:

- `LibVLCSharp.WPF`
- `VideoLAN.LibVLC.Windows`

## Run

```powershell
dotnet restore
dotnet build VideoCutter.Wpf.sln
dotnet run --project .\VideoCutter.Wpf\VideoCutter.Wpf.csproj
```

## Build Release

```powershell
dotnet publish .\VideoCutter.Wpf\VideoCutter.Wpf.csproj -c Release
```

## Notes

- Fast mode uses stream copy: `-c copy`
- Accurate mode re-encodes video and audio
- The app expects FFmpeg tooling to be installed on the machine

## Next Steps

Possible follow-up improvements:

- Frame-accurate timeline preview
- Multi-segment clipping
- Batch processing
- GPU encoding support
- Better progress parsing from FFmpeg output
