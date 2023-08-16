using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

class Program {

    static async Task Main(string[] args) {
        Console.WriteLine("SPACE - tag, Q - quit");

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

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

    static async Task<ShazamResult> CaptureAndTagAsync() {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        using(var capture = new WasapiCapture()) {
            var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };

            capture.DataAvailable += (s, e) => {
                captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            capture.StartRecording();

            using(var resampler = new MediaFoundationResampler(captureBuf, new WaveFormat(Analysis.SAMPLE_RATE, 16, 1))) {
                var sampleProvider = resampler.ToSampleProvider();
                var retryMs = 3000;
                var tagId = Guid.NewGuid().ToString();

                while(true) {
                    while(captureBuf.BufferedDuration.TotalSeconds < 1)
                        Thread.Sleep(100);

                    analysis.ReadChunk(sampleProvider);

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
        }
    }
}
