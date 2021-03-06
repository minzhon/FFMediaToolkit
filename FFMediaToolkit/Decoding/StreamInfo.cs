﻿namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents informations about the video stream.
    /// </summary>
    public class StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamInfo"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe StreamInfo(AVStream* stream, InputContainer container)
        {
            var codec = stream->codec;
            Metadata = new ReadOnlyDictionary<string, string>(FFDictionary.ToDictionary(stream->metadata));
            CodecName = ffmpeg.avcodec_get_name(codec->codec_id);
            CodecId = codec->codec_id.FormatEnum(12);
            Index = stream->index;
            IsInterlaced = codec->field_order != AVFieldOrder.AV_FIELD_PROGRESSIVE &&
                           codec->field_order != AVFieldOrder.AV_FIELD_UNKNOWN;
            FrameSize = new Size(codec->width, codec->height);
            PixelFormat = codec->pix_fmt.FormatEnum(11);
            AVPixelFormat = codec->pix_fmt;
            TimeBase = stream->time_base;
            RealFrameRate = stream->r_frame_rate;
            AvgFrameRate = stream->avg_frame_rate.ToDouble();
            IsVariableFrameRate = RealFrameRate.ToDouble() != AvgFrameRate;
            Duration = stream->duration >= 0
                ? stream->duration.ToTimeSpan(stream->time_base)
                : TimeSpan.FromTicks(container.Pointer->duration * 10);
            var start = stream->start_time.ToTimeSpan(stream->time_base);
            StartTime = start == TimeSpan.MinValue ? TimeSpan.Zero : start;

            if (stream->nb_frames > 0)
            {
                IsFrameCountProvidedByContainer = true;
                FrameCount = (int)stream->nb_frames;
            }
            else
            {
                FrameCount = Duration.ToFrameNumber(stream->avg_frame_rate);
            }
        }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the codec name.
        /// </summary>
        public string CodecName { get; }

        /// <summary>
        /// Gets the codec identifier.
        /// </summary>
        public string CodecId { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="FrameCount"/> value is know from the multimedia container metadata.
        /// </summary>
        public bool IsFrameCountProvidedByContainer { get; }

        /// <summary>
        /// Gets a value indicating whether the frames in the stream are interlaced.
        /// </summary>
        public bool IsInterlaced { get; }

        /// <summary>
        /// Gets a value indicating whether the video is variable frame rate (VFR).
        /// </summary>
        public bool IsVariableFrameRate { get; }

        /// <summary>
        /// Gets the average frame rate as a <see cref="double"/> value.
        /// </summary>
        public double AvgFrameRate { get; }

        /// <summary>
        /// Gets the frame rate as a <see cref="AVRational"/> value.
        /// It is used to calculate timestamps in the internal decoder methods.
        /// </summary>
        public AVRational RealFrameRate { get; }

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase { get; }

        /// <summary>
        /// Gets the video frame dimensions.
        /// </summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Gets a lowercase string representing the video pixel format.
        /// </summary>
        public string PixelFormat { get; }

        /// <summary>
        /// Gets the number of frames value from the container metadata, if available (see <see cref="IsFrameCountProvidedByContainer"/>)
        /// Otherwise, it is estimated from the video duration and average frame rate.
        /// This value may not be accurate, if the video is variable frame rate (see <see cref="IsVariableFrameRate"/> property).
        /// </summary>
        public int FrameCount { get; }

        /// <summary>
        /// Gets the stream duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the stream start time. Null if undefined.
        /// </summary>
        public TimeSpan? StartTime { get; }

        /// <summary>
        /// Gets the stream metadata.
        /// </summary>
        public ReadOnlyDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Gets the video pixel format.
        /// </summary>
        internal AVPixelFormat AVPixelFormat { get; }
    }
}