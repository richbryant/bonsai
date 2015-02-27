﻿using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Dsp
{
    [Combinator]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class ConvertFromArray
    {
        public Size Size { get; set; }

        public Depth? Depth { get; set; }

        public int? Channels { get; set; }

        static int ElementSize(Depth depth)
        {
            switch (depth)
            {
                case OpenCV.Net.Depth.U8:
                case OpenCV.Net.Depth.S8: return 1;
                case OpenCV.Net.Depth.U16:
                case OpenCV.Net.Depth.S16: return 2;
                case OpenCV.Net.Depth.S32:
                case OpenCV.Net.Depth.F32: return 4;
                case OpenCV.Net.Depth.F64: return 8;
                default: throw new ArgumentException("Invalid depth was specified.");
            }
        }

        static Mat FromArray(int rows, int cols, Depth depth, int channels, Array array)
        {
            var dataHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                var output = new Mat(rows, cols, depth, channels, dataHandle.AddrOfPinnedObject());
                return output.Clone();
            }
            finally { dataHandle.Free(); }
        }

        Mat FromArray<T>(T[] input, Depth defaultDepth)
        {
            var size = Size;
            var depth = Depth;
            var channels = Channels;
            if (size.Width > 0 || size.Height > 0 || depth.HasValue || channels.HasValue)
            {
                if (!depth.HasValue) depth = defaultDepth;
                if (!channels.HasValue) channels = 1;

                var rows = size.Height;
                var cols = size.Width;
                if (rows == 0 && cols == 0) rows = 1;
                if (rows == 0) rows = input.Length / (ElementSize(depth.Value) * channels.Value * cols);
                if (cols == 0) cols = input.Length / (ElementSize(depth.Value) * channels.Value * rows);
                return FromArray(rows, cols, depth.Value, channels.Value, input);
            }
            else return null;
        }

        public IObservable<Mat> Process(IObservable<byte[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.U8);
                return output ?? Mat.FromArray(input);
            });
        }

        public IObservable<Mat> Process(IObservable<short[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.S16);
                return output ?? Mat.FromArray(input);
            });
        }

        public IObservable<Mat> Process(IObservable<ushort[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.U16);
                return output ?? Mat.FromArray(input);
            });
        }

        public IObservable<Mat> Process(IObservable<int[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.S32);
                return output ?? Mat.FromArray(input);
            });
        }

        public IObservable<Mat> Process(IObservable<float[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.F32);
                return output ?? Mat.FromArray(input);
            });
        }

        public IObservable<Mat> Process(IObservable<double[]> source)
        {
            return source.Select(input =>
            {
                var output = FromArray(input, OpenCV.Net.Depth.F64);
                return output ?? Mat.FromArray(input);
            });
        }
    }
}