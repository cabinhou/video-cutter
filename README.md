# video-cutter-wpf

一个基于 `C#`、`WPF`、`FFmpeg` 和 `LibVLCSharp` 的本地桌面视频裁剪工具。

English README: [README.en.md](D:\AI-Code\video-cutter\README.en.md)

## 项目简介

这个仓库提供了一个 Windows 桌面端 MVP，用于完成基础的视频预览和片段裁剪。

当前已实现的功能：

- 打开本地视频文件
- 在应用内使用 `LibVLCSharp` 进行视频预览
- 播放、暂停、停止，以及按 5 秒步进快进或后退
- 拖动时间轴进行粗略定位
- 根据当前播放位置设置裁剪开始时间和结束时间
- 使用 `ffprobe` 读取视频时长等媒体信息
- 使用 `ffmpeg` 导出裁剪后的视频片段
- 支持快速流拷贝模式和精确重编码模式
- 显示处理状态、进度和 FFmpeg 日志

## 技术栈

- UI：`WPF`
- 语言：`C#`
- 视频预览：`LibVLCSharp.WPF`
- 媒体信息读取：`ffprobe`
- 视频裁剪：`ffmpeg`
- 目标框架：`.NET 8`

## 仓库结构

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

## 运行前准备

运行项目之前，请确保本机具备以下环境：

1. Windows
2. `.NET 8 SDK`
3. 已安装 `ffmpeg` 和 `ffprobe`，并已加入系统 `PATH`

项目使用的主要 NuGet 依赖：

- `LibVLCSharp.WPF`
- `VideoLAN.LibVLC.Windows`

## 本地运行

```powershell
dotnet restore
dotnet build VideoCutter.Wpf.sln
dotnet run --project .\VideoCutter.Wpf\VideoCutter.Wpf.csproj
```

## 发布构建

```powershell
dotnet publish .\VideoCutter.Wpf\VideoCutter.Wpf.csproj -c Release
```

## 使用说明

1. 打开应用后选择本地视频文件。
2. 程序会自动读取视频时长，并填充默认输出文件名。
3. 通过播放器和时间轴定位到目标位置，设置开始时间和结束时间。
4. 选择裁剪模式：
   - `FastStreamCopy`：速度更快，使用流拷贝，适合快速截取
   - `AccurateReencode`：精度更高，重新编码音视频，适合对裁剪准确性要求更高的场景
5. 选择输出路径后执行裁剪。
6. 在界面中查看日志、状态和进度。

## 说明

- 快速模式底层使用 `-c copy`
- 精确模式会重新编码视频和音频
- 程序依赖系统环境中的 FFmpeg 工具链
- 裁剪能力和兼容性会受到输入视频编码格式及 FFmpeg 行为影响

## 后续可扩展方向

- 帧级精确时间轴预览
- 多片段裁剪
- 批量处理
- GPU 编码支持
- 更精细的 FFmpeg 进度解析
