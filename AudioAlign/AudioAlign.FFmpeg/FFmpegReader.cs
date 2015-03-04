﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioAlign.FFmpeg {
    public class FFmpegReader : IDisposable {

        private static InteropWrapper interop;

        static FFmpegReader() {
            interop = new InteropWrapper();
        }

        private bool disposed = false;
        private IntPtr instance = IntPtr.Zero;
        private OutputConfig outputConfig;

        public FFmpegReader(string filename) {
            instance = interop.stream_open(filename);
            
            IntPtr ocp = interop.stream_get_output_config(instance);
            outputConfig = (OutputConfig)Marshal.PtrToStructure(ocp, typeof(OutputConfig));
        }

        public FFmpegReader(FileInfo fileInfo) : this(fileInfo.FullName) { }

        public OutputConfig OutputConfig {
            get { return outputConfig; }
        }

        public int ReadFrame(out long timestamp, out IntPtr data) {
            return interop.stream_read_frame(instance, out timestamp, out data);
        }

        public void Seek(long timestamp) {
            interop.stream_seek(instance, timestamp);
        }

        #region IDisposable & destructor

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// http://www.codeproject.com/KB/cs/idisposable.aspx
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                interop.stream_close(instance);
                instance = IntPtr.Zero;
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        ~FFmpegReader() {
            Dispose(false);
        }

        #endregion
    }
}
