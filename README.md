# FFMediaToolkit

[![Build status](https://ci.appveyor.com/api/projects/status/9vaaqchtx1d5nldj?svg=true)](https://ci.appveyor.com/project/radek41/ffmediatoolkit) [![Nuget](https://img.shields.io/nuget/v/FFMediaToolkit.svg)](https://www.nuget.org/packages/FFMediaToolkit/)
[![License](https://img.shields.io/github/license/radek-k/FFMediaToolkit.svg)](https://github.com/radek-k/FFMediaToolkit/blob/master/LICENSE)

**FFMediaToolkit** is a **cross-platform** **.NET Standard** library for **creating and reading video files**. It uses native **FFmpeg** libraries by the [FFmpeg.Autogen](https://github.com/Ruslan-B/FFmpeg.AutoGen) bindings.

## Features

- **Decoding/encoding videos** in almost any format supported by FFmpeg.
- **Fast<sup id="a1">[1](#f1)</sup>, frame accurate access to any video frame** by frame index<sup id="a1">[2](#f1)</sup> or timestamp.
- **Creating videos from images** with metadata, pixel format, bitrate, CRF, FPS, GoP, dimensions and other codec settings.
- **Compatible with most of .NET graphics libraries**<sup id="a1">[3](#f1)</sup>.
- Supports reading multimedia chapters and metadata.
- **Simple, object-oriented, easy-to-use API** with inline documentation.
- **Cross-platform** - works on **Linux**, **Windows** and **MacOS** - with **.NET Core** or **.NET Framework** projects.
_____
<b id="f1">1</b> The time it takes to obtain a frame depends on the number of keyframes in the video stream(see https://en.wikipedia.org/wiki/Key_frame#Video_compression)  
<b id="f1">2</b> Access to frame by index is not supported in Variable Frame Rate videos.  
<b id="f3">3</b> See the [usage details](https://github.com/radek-k/FFMediaToolkit#usage-details)
## Code samples

- Extract all video frames as PNG files

    ````c#
    var file = MediaFile.Open(@"C:\videos\movie.mp4");
    for (int i = 0; i < file.Video.Info.FrameCount; i++)
    {
        file.Video.ReadFrame(i).ToBitmap().Save($@"C:\images\frame_{i}.png");
        // See the #Usage details for example .ToBitmap() implementation
        // The .Save() method may be different depending on your graphics library
    }
    ````
- Video decoding

    ````c#
    // Opens a multimedia file.
    // You can use the MediaOptions properties to set decoder options.
    var file = MediaFile.Open(@"C:\videos\movie.mp4");
    
    // Print informations about the video stream.
    Console.WriteLine($"Bitrate: {file.Info.Bitrate / 1000.0} kb/s");
    var info = file.Video.Info;
    Console.WriteLine($"Duration: {info.Duration}");
    var isFrameCountAccurate = info.IsFrameCountProvidedByContainer || !info.IsVariableFrameRate;
    var frameCount = isFrameCountAccurate ? info.FrameCount.ToString() : "N/A";
    Console.WriteLine($"Frames count: {frameCount}");
    var frameRateInfo = info.IsVariableFrameRate ? "average" : "constant";
    Console.WriteLine($"Frame rate: {info.AvgFrameRate} fps ({frameRateInfo})");
    Console.WriteLine($"Frame size: {info.FrameSize}");
    Console.WriteLine($"Pixel format: {info.PixelFormat}");
    Console.WriteLine($"Codec: {info.CodecName}");
    Console.WriteLine($"Is interlaced: {info.IsInterlaced}");

    // Gets a frame by its number.
    var frame102 = file.Video.ReadFrame(frameNumber: 102);

    // Gets the frame at 5th second of the video.
    var frame5s = file.Video.ReadFrame(TimeSpan.FromSeconds(5));
    ````

- Encode video from images.
    
    ````c#
    // You can set there codec, bitrate, frame rate and many other options.
    var settings = new VideoEncoderSettings(width: 1920, height: 1080, framerate: 30, codec: VideoCodec.H264);
    settings.EncoderPreset = EncoderPreset.Fast;
    settings.CRF = 17;
    var file = MediaBuilder.CreateContainer(@"C:\videos\example.mp4").WithVideo(settings).Create();
    while(file.Video.FramesCount < 300)
    {
        file.Video.AddFrame(/*Your code*/);
    }
    file.Dispose(); // MediaOutput ("file" variable) must be disposed when encoding is completed. You can use `using() { }` block instead.
    ````

## Setup

- Install the **FFMediaToolkit** package from [NuGet](https://www.nuget.org/packages/FFMediaToolkit/).

    ````shell
    dotnet add package FFMediaToolkit
    ````

    ````Package Manager Console
    PM> Install-Package FFMediaToolkit
    ````

> **FFmpeg libraries are not included with the package.** To use FFMediaToolkit, you need the **latest FFmpeg (>= v4.2) shared build** binaries. You can download it from the [Zeranoe FFmpeg](https://ffmpeg.zeranoe.com/builds/) site or build your own.

> FFmpeg libraries must have the same architecture as your project. If you want to use 64-bit FFmpeg, you should disable the *Build* -> *Prefer 32-bit* option in Visual Studio project properties.
- Required FFmpeg binaries (dll/so/dylib):
  - **avcodec** v58
  - **avformat** v58
  - **avutil** v56
  - **swresample** v3
  - **swscale** v5
- FFmpeg setup:
  - **Windows** - Place the binaries in the `.\ffmpeg\x86\` (32 bit) and `.\ffmpeg\x86_64\`(64bit) in the application output directory.
  - **Linux** - FFmpeg is pre-installed on many desktop Linux systems. The default path is `/usr/lib/x86` (`_64`) `-linux-gnu/`.
  - **MacOS** - You can install FFmpeg via MacPorts or download `.dylib` files from the [Zeranoe](https://ffmpeg.zeranoe.com/builds/) site. The default path is `/opt/local/lib/`.

  If you want to **use other directory**, you can **specify a path to it** by the  `FFmpegLoader.FFmpegPath` property.

## Usage details

FFMediaToolkit uses the [*ref struct*](https://docs.microsoft.com/pl-pl/dotnet/csharp/language-reference/keywords/ref#ref-struct-types) `ImageData` for bitmap images. The `.Data` property contains pixels data in a [`Span<byte>`](https://docs.microsoft.com/pl-pl/dotnet/api/system.span-1?view=netstandard-2.1). 
> **If you want to process or save the `ImageData`, you should convert it to another graphics object using the following methods.**

> **These methods are not included in the program to avoid additional dependencies and provide compatibility with many graphic libraries.**

- **For [ImageSharp](https://github.com/SixLabors/ImageSharp) library (.NET Standard/Core - cross-platform):**

    ````c#
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    ...
    public static Image<Bgr24> ToBitmap(this ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }
    ````

- **For .NET Framework `System.Drawing.Bitmap` (Windows only):**

    ````c#
    // ImageData -> Bitmap (unsafe)
    public static unsafe Bitmap ToBitmap(this ImageData bitmap)
    {
        fixed(byte* = bitmap.Data)
        {
            return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(bitmap.Data));
        }
    }

    // Bitmap -> ImageData (safe)
    public static ImageData ToImageData(this Bitmap bitmap)
    {
        var rect = new Rectangle(Point.Empty, bitmap.Size);
        var bitLock = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        var bitmapData = ImageData.FromPointer(bitLock.Scan0, bitmap.Size, ImagePixelFormat.Bgr24);
        bitmap.UnlockBits(bitLock);
        return bitmapData;
    }
    ````

- **For .NET Framework/Core desktop apps with WPF UI. (Windows only):**

    ````c#
    using System.Windows.Media.Imaging;
    ...
    // ImageData -> BitmapSource (unsafe)
    public static unsafe BitmapSource ToBitmap(this ImageData bitmapData)
    {
        fixed(byte* ptr = bitmapData.Data)
        {
            return BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96, PixelFormats.Bgr32, null, new IntPtr(ptr), bitmapData.Data.Length, bitmapData.Stride);
        }
    }

    // BitmapSource -> ImageData (safe)
    public static ImageData ToImageData(this BitmapSource bitmap)
    {
        var wb = new WriteableBitmap(bitmap);
        return ImageData.FromPointer(wb.BackBuffer, ImagePixelFormat.Bgra32, wb.PixelWidth, wb.PixelHeight);
    }
    ````
- **FFMediaToolkit will also work with any other graphics library that supports creating images from `Span<byte>`, byte array or memory pointer**

## Licensing

This project is licensed under the [MIT](https://github.com/radek-k/FFMediaToolkit/blob/master/LICENSE) license.
