﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public struct SubFingerprint {

        private static UInt32[] bitMasks;

        static SubFingerprint() {
            bitMasks = new UInt32[32];
            for (int x = 0; x < 32; x++) {
                bitMasks[x] = 1u << x;
            }
        }

        private UInt32 value;

        public SubFingerprint(UInt32 value) {
            this.value = value;
        }

        public UInt32 Value { 
            get { return this.value; } 
        }

        public bool this[int bit] {
            get {
                if (bit < 0 || bit > 31) {
                    throw new IndexOutOfRangeException();
                }
                return (this.value & bitMasks[bit]) == bitMasks[bit];
            }
            set {
                if (bit < 0 || bit > 31) {
                    throw new IndexOutOfRangeException();
                }
                if (value == true) {
                    this.value |= bitMasks[bit];
                }
                else {
                    this.value &= ~bitMasks[bit];
                }
            }
        }

        public override bool Equals(object obj) {
            return this.value.Equals(((SubFingerprint)obj).value);
        }

        public override int GetHashCode() {
            return this.value.GetHashCode();
        }

        public override string ToString() {
            return "SubFingerprint {" + Convert.ToString(this.value, 2).PadLeft(32, '0') + " (" + this.value + ")}";
        }

        /// <summary>
        /// Hamming Distance algorithm taken from: http://en.wikipedia.org/wiki/Hamming_distance#Algorithm_example
        /// </summary>
        /// <param name="sfp"></param>
        /// <returns></returns>
        public uint HammingDistance(SubFingerprint sfp) {
            uint i = this.value ^ sfp.value;
            // Count the number of set bits: http://stackoverflow.com/a/109025
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static bool operator !=(SubFingerprint a, SubFingerprint b) {
            return a.value != b.value;
        }

        public static bool operator ==(SubFingerprint a, SubFingerprint b) {
            return a.value == b.value;
        }

        public SubFingerprint Difference(SubFingerprint subFingerprint) {
            return new SubFingerprint(this.value ^ subFingerprint.value);
        }
    }
}
