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
        static readonly string TEST_DATA_DIR;

        static SignatureComparisonTests() {
            TEST_DATA_DIR = Path.Combine(
                Path.GetDirectoryName(typeof(SignatureComparisonTests).Assembly.Location),
                "../../../TestData"
            );
        }

        [Fact]
        public void Ref_SigX_10_1_3() {
            CreateFromWaveFile(
                Path.Combine(TEST_DATA_DIR, "test2.wav"),
                out var mySampleCount,
                out var myPeaks
            );

            LoadBinary(
                // Generated using libsigx.so from Android app v13.45
                Path.Combine(TEST_DATA_DIR, "test2-sigx-10.1.3.bin"),
                out var refSampleCount,
                out var refPeaks
            );

            // TODO
            // https://github.com/marin-m/SongRec/blob/0.3.2/src/fingerprinting/signature_format.rs#L128
            Assert.Equal(0.24, Math.Round(1d * (refSampleCount - mySampleCount) / Analysis.SAMPLE_RATE, 2));

            var anchorBin = 38;
            var myStripe = myPeaks.FindOne(anchorBin).StripeIndex;
            var refStripe = refPeaks.FindOne(anchorBin).StripeIndex;
            var refStripeOffset = refStripe - myStripe;

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

                myMagnList.Add(myPeak.InterpolatedLogMagnitude);
                refMagnList.Add(refPeak.InterpolatedLogMagnitude);
            }

            Assert.True(1d * hitCount / myPeaks.Count > 0.8);

            var (magnFitIntercept, magnFitSlope) = Fit.Line(myMagnList.ToArray(), refMagnList.ToArray());

            Assert.True(Math.Abs(magnFitSlope - 1) < 0.01);
            Assert.True(Math.Abs(magnFitIntercept) < 100);
        }

        static void CreateFromWaveFile(string path, out int sampleCount, out IReadOnlyList<PeakInfo> peaks) {
            var analysis = new Analysis();
            var finder = new PeakFinder(analysis);

            using var wave = new WaveFileReader(path);
            var sampleProvider = wave.ToSampleProvider();

            var chunk = new float[Analysis.CHUNK_SIZE];

            while(true) {
                if(sampleProvider.Read(chunk, 0, chunk.Length) < chunk.Length)
                    break;

                analysis.AddChunk(chunk);
                finder.Find();
            }

            sampleCount = analysis.ProcessedSamples;
            peaks = finder.EnumerateAllPeaks().ToList();
        }


        static void LoadBinary(string path, out int sampleCount, out IReadOnlyList<PeakInfo> peaks) {
            Sig.Read(
                File.ReadAllBytes(path),
                out var sampleRate,
                out sampleCount,
                out peaks
            );

            Assert.Equal(Analysis.SAMPLE_RATE, sampleRate);
        }
    }

}
#endif
