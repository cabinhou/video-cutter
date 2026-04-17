# video-cutter-wpf

A local desktop video trimming tool built with `C#`, `WPF`, `FFmpeg`, and `LibVLCSharp`.

中文说明: [README.md](D:\AI-Code\video-cutter\README.md)

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
|-- README.en.md
|-- LICENSE
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

Main NuGet dependencies used by the project:

- `LibVLCSharp.WPF`
- `VideoLAN.LibVLC.Windows`

## Run Locally

```powershell
dotnet restore
dotnet build VideoCutter.Wpf.sln
dotnet run --project .\VideoCutter.Wpf\VideoCutter.Wpf.csproj
```

## Build Release

```powershell
dotnet publish .\VideoCutter.Wpf\VideoCutter.Wpf.csproj -c Release
```

## Usage

1. Open the application and select a local video file.
2. The app reads the video duration and fills a default output filename.
3. Use the player and timeline to locate the target range, then set the start and end times.
4. Choose a cutting mode:
   - `FastStreamCopy`: faster, uses stream copy, suitable for quick cuts
   - `AccurateReencode`: higher precision, re-encodes audio and video, suitable when cut accuracy matters more
5. Choose the output path and start the cut.
6. Review logs, status, and progress in the UI.

## Notes

- Fast mode uses `-c copy`
- Accurate mode re-encodes video and audio
- The app depends on FFmpeg tools installed on the system
- Cutting behavior and compatibility depend on the input codec and FFmpeg behavior

## Possible Next Steps

- Frame-accurate timeline preview
- Multi-segment clipping
- Batch processing
- GPU encoding support
- Better FFmpeg progress parsing
