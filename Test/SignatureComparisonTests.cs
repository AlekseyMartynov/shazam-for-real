#if DEBUG
using MathNet.Numerics;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Project.Test {

    public class SignatureComparisonTests {

        [Fact]
        public void Ref_SigX_10_1_3() {
            CreateFromWaveFile(
                Path.Combine(TestHelper.DATA_DIR, "test2.wav"),
                out var mySampleCount,
                out var myRemainingSampleCount,
                out var myPeaks
            );

            LoadBinary(
                // Generated using libsigx.so from Android app v13.45
                Path.Combine(TestHelper.DATA_DIR, "test2-sigx-10.1.3.bin"),
                out var refSampleCount,
                out var refPeaks
            );

            // Official signature has more peaks
            // Most of them are at edges < RADIUS_TIME

            // Possible explanation for padding in official signature
            // start - first chunk completes the FFT window so it is immediately ready for analysis
            // end - to absorb remaining samples? for symmetry?

            var refSampleCountPadding = Analysis.WINDOW_SIZE - Analysis.CHUNK_SIZE;
            var refStripeOffset = refSampleCountPadding / Analysis.CHUNK_SIZE;

            Assert.Equal(refSampleCount - 2 * refSampleCountPadding, mySampleCount + myRemainingSampleCount);

            var hitCount = 0;
            var missCount = 0;

            var myMagnList = new List<double>();
            var refMagnList = new List<double>();

            foreach(var myPeak in myPeaks) {
                var refPeak = refPeaks.FindOne(myPeak.StripeIndex + refStripeOffset, myPeak.InterpolatedBin);

                if(refPeak == null) {
                    missCount++;
                    continue;
                }

                hitCount++;

                myMagnList.Add(myPeak.LogMagnitude);
                refMagnList.Add(refPeak.LogMagnitude);
            }

            Assert.True(1d * hitCount / myPeaks.Count > 0.8);

            var (magnFitIntercept, magnFitSlope) = Fit.Line(myMagnList.ToArray(), refMagnList.ToArray());

            Assert.True(Math.Abs(magnFitSlope - 1) < 0.001);
            Assert.True(Math.Abs(magnFitIntercept) < 10);
        }

        static void CreateFromWaveFile(string path, out int sampleCount, out int remainingSampleCount, out IReadOnlyList<PeakInfo> peaks) {
            var analysis = new Analysis();
            var finder = new PeakFinder(analysis);

            using var wave = new WaveFileReader(path);
            var sampleProvider = wave.ToSampleProvider();

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
            LoadBinary(sigBytes, out sampleCount, out peaks);
        }


        static void LoadBinary(string path, out int sampleCount, out IReadOnlyList<PeakInfo> peaks) {
            LoadBinary(File.ReadAllBytes(path), out sampleCount, out peaks);
        }

        static void LoadBinary(byte[] data, out int sampleCount, out IReadOnlyList<PeakInfo> peaks) {
            Sig.Read(
                data,
                out var sampleRate,
                out sampleCount,
                out peaks
            );

            Assert.Equal(Analysis.SAMPLE_RATE, sampleRate);
        }
    }

}
#endif
