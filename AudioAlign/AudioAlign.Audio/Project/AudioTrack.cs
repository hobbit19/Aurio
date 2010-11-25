﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AudioAlign.Audio.Project {
    public class AudioTrack: Track {

        static AudioTrack() {
            MediaType = MediaType.Audio;
        }

        public AudioTrack(FileInfo fileInfo) : base(fileInfo) { }
        public AudioTrack(FileInfo fileInfo, TimeSpan length, TimeSpan offset) : base(fileInfo, length, offset) { }

        public IAudioStream16 CreateAudioStream() {
            return AudioStreamFactory.FromFileInfo(FileInfo);
        }

        public FileInfo PeakFile {
            get {
                return new FileInfo(FileInfo.FullName + AudioStreamFactory.PEAKFILE_EXTENSION);
            }
        }

        public bool HasPeakFile {
            get {
                return PeakFile.Exists;
            }
        }
    }
}