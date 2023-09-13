﻿#if DEBUG
using MathNet.Numerics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Project.Test {
    using Bands = IReadOnlyList<IReadOnlyList<PeakInfo>>;

    public class SignatureComparisonTests {

        [Fact]
        public void Ref_SigX_10_1_3() {
            CreateFromFile(
                Path.Combine(TestHelper.DATA_DIR, "test.mp3"),
                out var mySampleCount,
                out var myRemainingSampleCount,
                out var myBands
            );

            LoadBinary(
                // Generated using libsigx.so from Android app v13.45
                Path.Combine(TestHelper.DATA_DIR, "test-sigx-10.1.3.bin"),
                out var refSampleCount,
                out var refBands
            );

            Assert.Equal(refSampleCount, mySampleCount + myRemainingSampleCount);

            var hitCount = 0;
            var missCount = 0;

            var myMagnList = new List<double>();
            var refMagnList = new List<double>();

            var myPeaks = myBands.SelectMany(i => i).ToList();
            var refPeaks = refBands.SelectMany(i => i).ToList();

            foreach(var myPeak in myPeaks) {
                var refPeak = refPeaks.FindOne(myPeak.StripeIndex, myPeak.InterpolatedBin);

                if(refPeak == null) {
                    missCount++;
                    continue;
                }

                hitCount++;

                myMagnList.Add(myPeak.LogMagnitude);
                refMagnList.Add(refPeak.LogMagnitude);
            }

            Assert.True(1d * hitCount / myPeaks.Count > 0.93);

            var (magnFitIntercept, magnFitSlope) = Fit.Line(myMagnList.ToArray(), refMagnList.ToArray());

            Assert.True(Math.Abs(magnFitSlope - 1) < 0.001);
            Assert.True(Math.Abs(magnFitIntercept) < 10);
        }

        static void CreateFromFile(string path, out int sampleCount, out int remainingSampleCount, out Bands bands) {
            var analysis = new Analysis();
            var finder = new PeakFinder(analysis);

            using var captureHelper = new FileCaptureHelper(path);
            captureHelper.Start();

            var sampleProvider = AddPadding(captureHelper.SampleProvider);
            var chunk = new float[Analysis.CHUNK_SIZE];

            while(true) {
                var readCount = sampleProvider.Read(chunk, 0, chunk.Length);

                if(readCount < chunk.Length) {
                    remainingSampleCount = readCount;
                    break;
                }

                analysis.AddChunk(chunk);
            }

            var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
            LoadBinary(sigBytes, out sampleCount, out bands);
        }


        static void LoadBinary(string path, out int sampleCount, out Bands bands) {
            LoadBinary(File.ReadAllBytes(path), out sampleCount, out bands);
        }

        static void LoadBinary(byte[] data, out int sampleCount, out Bands bands) {
            Sig.Read(
                data,
                out var sampleRate,
                out sampleCount,
                out bands
            );

            Assert.Equal(Analysis.SAMPLE_RATE, sampleRate);
        }

        static ISampleProvider AddPadding(ISampleProvider sampleProvider) {
            // Possible explanation for padding in official signature
            //   start : first chunk completes the FFT window so it is immediately ready for analysis
            //   end   : to absorb remaining samples? for symmetry?

            var waveFormat = sampleProvider.WaveFormat;
            var sampleCount = Analysis.WINDOW_SIZE - Analysis.CHUNK_SIZE;

            return new ConcatenatingSampleProvider(new[] {
                new FixedLenSilence(waveFormat, sampleCount),
                sampleProvider,
                new FixedLenSilence(waveFormat, sampleCount),
            });
        }

        class FixedLenSilence : ISampleProvider {
            int SamplesLeft;

            public FixedLenSilence(WaveFormat waveFormat, int sampleCount) {
                WaveFormat = waveFormat;
                SamplesLeft = sampleCount;
            }

            public WaveFormat WaveFormat { get; private set; }

            public int Read(float[] buffer, int offset, int count) {
                count = Math.Min(count, SamplesLeft);
                Array.Clear(buffer, offset, count);
                SamplesLeft -= count;
                return count;
            }
        }

    }

}
#endif
