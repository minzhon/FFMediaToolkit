﻿namespace FFMediaToolkit.Encoding.Internal
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains method for creating media streams.
    /// </summary>
    internal static unsafe class OutputStreamFactory
    {
        /// <summary>
        /// Creates a new video stream for the specified <see cref="OutputContainer"/>.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="config">The stream settings.</param>
        /// <returns>The new video stream.</returns>
        public static OutputStream<VideoFrame> CreateVideo(OutputContainer container, VideoEncoderSettings config)
        {
            var codecId = config.Codec == VideoCodec.Default ? container.Pointer->oformat->video_codec : (AVCodecID)config.Codec;

            if (codecId == AVCodecID.AV_CODEC_ID_NONE)
                throw new InvalidOperationException("The media container doesn't support video!");

            var codec = ffmpeg.avcodec_find_encoder(codecId);

            if (codec == null)
                throw new InvalidOperationException($"Cannot find an encoder with the {codecId}!");

            if (codec->type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new InvalidOperationException($"The {codecId} encoder doesn't support video!");

            var videoStream = ffmpeg.avformat_new_stream(container.Pointer, codec);
            videoStream->time_base = new AVRational { num = config.FramerateRational.den, den = config.FramerateRational.num }; // frame rate (x/1) to time base (1/x) conversion;
            var codecContext = videoStream->codec;
            codecContext->codec_id = codecId;
            codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;

            codecContext->width = config.VideoWidth;
            codecContext->height = config.VideoHeight;

            codecContext->time_base = videoStream->time_base;
            codecContext->gop_size = config.KeyframeRate;
            codecContext->pix_fmt = (AVPixelFormat)config.VideoFormat;

            if ((container.Pointer->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            var dict = new FFDictionary(config.CodecOptions);

            if (config.CRF.HasValue && config.Codec.IsMatch(VideoCodec.H264, VideoCodec.H265, VideoCodec.VP9, VideoCodec.VP8))
            {
                dict["crf"] = config.CRF.Value.ToString();
            }
            else
            {
                codecContext->bit_rate = config.Bitrate;
            }

            if (config.Codec.IsMatch(VideoCodec.H264, VideoCodec.H265))
            {
                dict["preset"] = config.EncoderPreset.GetDescription();
            }

            var ptr = dict.Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &ptr);

            dict.Update(ptr);

            return new OutputStream<VideoFrame>(videoStream, container);
        }
    }
}
