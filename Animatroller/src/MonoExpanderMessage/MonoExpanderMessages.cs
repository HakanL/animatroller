using System;
using System.Collections.Generic;

namespace Animatroller.Framework.MonoExpanderMessages
{
    public enum AudioTypes
    {
        Background,
        Track,
        Effect
    }

    public enum FileTypes
    {
        AudioBackground,
        AudioTrack,
        AudioEffect,
        Video
    }

    public class Ping
    {
    }

    //
    // Notify messages
    //

    public class InputChanged
    {
        public string Input { get; set; }

        public double Value { get; set; }
    }

    public class VideoPositionChanged
    {
        public string Id { get; set; }

        public double Position { get; set; }
    }

    public class AudioPositionChanged
    {
        public string Id { get; set; }

        public double Position { get; set; }

        public AudioTypes Type { get; set; }
    }

    public class VideoStarted
    {
        public string Id { get; set; }
    }

    public class VideoFinished
    {
        public string Id { get; set; }
    }

    public class AudioStarted
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }

    public class AudioFinished
    {
        public string Id { get; set; }

        public AudioTypes Type { get; set; }
    }

    //
    // Request messages
    //

    public class SetOutputRequest
    {
        public string Output { get; set; }

        public double Value { get; set; }

    }

    public class SendSerialRequest
    {
        public int Port { get; set; }

        public byte[] Data { get; set; }
    }

    public class AudioEffectCue
    {
        public string FileName { get; set; }
    }

    public class AudioEffectPlay
    {
        public string FileName { get; set; }

        public double? VolumeLeft { get; set; }

        public double? VolumeRight { get; set; }

        public bool Simultaneous { get; set; }
    }

    public class AudioEffectPause
    {
    }

    public class AudioEffectResume
    {
    }

    public class AudioBackgroundSetVolume
    {
        public double Volume { get; set; }
    }

    public class AudioEffectSetVolume
    {
        public double Volume { get; set; }
    }

    public class AudioTrackSetVolume
    {
        public double Volume { get; set; }
    }

    public class AudioBackgroundResume
    {
    }

    public class AudioBackgroundPause
    {
    }

    public class AudioBackgroundNext
    {
    }

    public class AudioTrackPlay
    {
        public string FileName { get; set; }
    }

    public class AudioTrackCue
    {
        public string FileName { get; set; }
    }

    public class AudioTrackResume
    {
    }

    public class AudioTrackPause
    {
    }

    public class VideoPlay
    {
        public string FileName { get; set; }
    }

    //
    // File requests
    //

    public class FileRequest
    {
        public string DownloadId { get; set; }

        public FileTypes Type { get; set; }

        public string FileName { get; set; }
    }

    public class FileResponse
    {
        public string DownloadId { get; set; }

        public long Size { get; set; }

        public byte[] SignatureSha1 { get; set; }
    }

    public class FileChunkRequest
    {
        public string DownloadId { get; set; }

        public FileTypes Type { get; set; }

        public string FileName { get; set; }

        public long ChunkStart { get; set; }

        public int ChunkSize { get; set; }
    }

    public class FileChunkResponse
    {
        public string DownloadId { get; set; }

        public long ChunkStart { get; set; }

        public byte[] Chunk { get; set; }
    }
}
