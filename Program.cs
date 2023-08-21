using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

class Program {

    static async Task Main(string[] args) {
        PrintHotkeys();
        CaptureSourceHelper.Set(false);

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

#if !MCI_CAPTURE
            if(Char.ToLower(key.KeyChar) == 's') {
                CaptureSourceHelper.Toggle();
                continue;
            }
#endif

            if(key.Key == ConsoleKey.Spacebar) {
                Console.Write("Listening... ");

                try {
                    var result = await CaptureAndTagAsync();

                    if(result.Success) {
                        Console.CursorLeft = 0;
                        Console.WriteLine(result.Url);
                        Process.Start("explorer", result.Url);
                    } else {
                        Console.WriteLine(":(");
                    }
                } catch(Exception x) {
                    Console.WriteLine("error: " + x.Message);
                }
            }
        }

    }

    static void PrintHotkeys() {
        var list = new List<string> {
            "SPACE - tag"
        };

#if !MCI_CAPTURE
        list.Add("S - source");
#endif
        list.Add("Q - quit");
        Console.WriteLine(String.Join(", ", list));
    }

    static async Task<ShazamResult> CaptureAndTagAsync() {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        using var captureHelper = CreateCaptureHelper();
        captureHelper.Start();

        var chunk = new float[Analysis.CHUNK_SIZE];
        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        while(true) {
            ReadChunk(captureHelper, chunk, out var sampleProviderChanged);

            if(sampleProviderChanged) {
                analysis = new Analysis();
                finder = new LandmarkFinder(analysis);
                continue;
            }

            analysis.AddChunk(chunk);

            if(analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

            if(analysis.ProcessedMs >= retryMs) {
                //new Painter(analysis, finder).Paint("c:/temp/spectro.png");
                //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");

                var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                var result = await ShazamApi.SendRequestAsync(tagId, analysis.ProcessedMs, sigBytes);
                if(result.Success)
                    return result;

                retryMs = result.RetryMs;
                if(retryMs == 0)
                    return result;
            }
        }
    }

    static ICaptureHelper CreateCaptureHelper() {
        var format = new WaveFormat(Analysis.SAMPLE_RATE, 16, 1);
#if MCI_CAPTURE
        return new MciCaptureHelper(format);
#else
        return new WasapiCaptureHelper(format);
#endif
    }

    static void ReadChunk(ICaptureHelper captureHelper, float[] chunk, out bool sampleProviderChanged) {
        var sampleProvider = captureHelper.SampleProvider;
        var offset = 0;
        var expectedCount = chunk.Length;

        sampleProviderChanged = false;

        while(true) {
            if(captureHelper.SampleProvider != sampleProvider) {
                sampleProviderChanged = true;
                return;
            }

            var actualCount = sampleProvider.Read(chunk, offset, expectedCount);

            if(actualCount == expectedCount)
                return;

            offset += actualCount;
            expectedCount -= actualCount;

            Thread.Sleep(100);
        }
    }
}
