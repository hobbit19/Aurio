﻿using Aurio.FFmpeg;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.Audio.Streams {
    public class FFmpegSourceStream : IAudioStream {

        private FFmpegReader reader;
        private AudioProperties properties;
        private long readerPosition; // samples
        private long readerFirstPTS; // samples

        private byte[] sourceBuffer;
        private int sourceBufferLength; // samples
        private int sourceBufferPosition; // samples


        public FFmpegSourceStream(FileInfo fileInfo) {
            reader = new FFmpegReader(fileInfo);

            if (reader.OutputConfig.length == long.MinValue) {
                /* 
                 * length == FFmpeg AV_NOPTS_VALUE
                 * 
                 * This means that for the opened file/format, there is no length/PTS data 
                 * available, which also makes seeking more or less impossible.
                 * 
                 * As a workaround, an index could be created to map the frames to the file
                 * position, and then seek by file position. The index could be created by 
                 * linearly reading through the file (decoding not necessary), and creating
                 * a mapping of AVPacket.pos to the frame time.
                 */
                throw new FileNotSeekableException();
            }

            properties = new AudioProperties(
                reader.OutputConfig.format.channels,
                reader.OutputConfig.format.sample_rate,
                reader.OutputConfig.format.sample_size * 8,
                reader.OutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM);

            readerPosition = 0;
            sourceBuffer = new byte[reader.OutputConfig.frame_size * 
                reader.OutputConfig.format.channels * 
                reader.OutputConfig.format.sample_size];
            sourceBufferPosition = 0;
            sourceBufferLength = -1; // -1 means buffer empty, >= 0 means valid buffer data

            // determine first PTS to handle cases where it is > 0
            try {
                Position = 0;
            }
            catch(InvalidOperationException) {
                readerFirstPTS = readerPosition;
                readerPosition = 0;
                Console.WriteLine("first PTS = " + readerFirstPTS);
            }
        }

        public AudioProperties Properties {
            get { return properties; }
        }

        public long Length {
            get { return reader.OutputConfig.length * properties.SampleBlockByteSize; }
        }

        private long SamplePosition {
            get { return readerPosition + sourceBufferPosition; }
        }

        public long Position {
            get {
                return SamplePosition * SampleBlockSize;
            }
            set {
                long seekTarget = (value / SampleBlockSize) + readerFirstPTS;

                // seek to target position
                reader.Seek(seekTarget);

                // get target position
                sourceBufferLength = reader.ReadFrame(out readerPosition, sourceBuffer, sourceBuffer.Length);

                // check if seek ended up at seek target (or earlier because of frame size, depends on file format and stream codec)
                // TODO handle seek offset with bufferPosition
                if (seekTarget == readerPosition) {
                    // perfect case
                    sourceBufferPosition = 0;
                }
                else if(seekTarget > readerPosition && seekTarget <= (readerPosition + sourceBufferLength)) {
                    sourceBufferPosition = (int)(seekTarget - readerPosition);
                }
                else if (seekTarget < readerPosition) {
                    throw new InvalidOperationException("illegal state");
                }

                // seek back to seek point for successive reads to return expected data (not one frame in advance) PROBABLY NOT NEEDED
                // TODO handle this case, e.g. when it is necessery and when it isn't (e.g. when block is chached because of bufferPosition > 0)
                //reader.Seek(readerPosition);
            }
        }

        public int SampleBlockSize {
            get { return properties.SampleBlockByteSize; }
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (sourceBufferLength == -1) {
                long newPosition;
                sourceBufferLength = reader.ReadFrame(out newPosition, sourceBuffer, sourceBuffer.Length);

                if (newPosition == -1 || sourceBufferLength == -1) {
                    return 0; // end of stream
                }

                readerPosition = newPosition;
                sourceBufferPosition = 0;
            }

            int bytesToCopy = Math.Min(count, (sourceBufferLength - sourceBufferPosition) * SampleBlockSize);
            Array.Copy(sourceBuffer, sourceBufferPosition * SampleBlockSize, buffer, offset, bytesToCopy);
            sourceBufferPosition += (bytesToCopy / SampleBlockSize);
            if (sourceBufferPosition > sourceBufferLength) {
                throw new InvalidOperationException("overflow");
            }
            else if (sourceBufferPosition == sourceBufferLength) {
                // buffer read completely, need to read next frame
                sourceBufferLength = -1;
            }

            return bytesToCopy;
        }

        public static FileInfo CreateWaveProxy(FileInfo fileInfo) {
            var outputFileInfo = new FileInfo(fileInfo.FullName + ".ffproxy.wav");

            if (outputFileInfo.Exists) {
                Console.WriteLine("Proxy already existing, using " + outputFileInfo.Name);
                return outputFileInfo;
            }

            var reader = new FFmpegReader(fileInfo);

            // workaround to get NAudio WaveFormat (instead of creating it manually here)
            var mss = new MemorySourceStream(null, new AudioProperties(
                reader.OutputConfig.format.channels, 
                reader.OutputConfig.format.sample_rate, 
                reader.OutputConfig.format.sample_size * 8, 
                reader.OutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM));
            var nass = new NAudioSinkStream(mss);
            var waveFormat = nass.WaveFormat;

            var writer = new WaveFileWriter(outputFileInfo.FullName, waveFormat);

            int output_buffer_size = reader.OutputConfig.frame_size * mss.SampleBlockSize;
            byte[] output_buffer = new byte[output_buffer_size];

            int samplesRead;
            long timestamp;

            // sequentially read samples from decoder and write it to wav file
            while ((samplesRead = reader.ReadFrame(out timestamp, output_buffer, output_buffer_size)) > 0) {
                int bytesRead = samplesRead * mss.SampleBlockSize;
                writer.Write(output_buffer, 0, bytesRead);
            }

            reader.Dispose();
            writer.Close();

            return outputFileInfo;
        }

        public class FileNotSeekableException : Exception {
            public FileNotSeekableException() : base() { }
            public FileNotSeekableException(string message) : base(message) { }
            public FileNotSeekableException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
