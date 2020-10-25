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

    static void Main(string[] args) {
        Console.WriteLine("SPACE - tag, Q - quit");

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

            if(key.Key == ConsoleKey.Spacebar) {
                Console.Write("Listening... ");

                var result = CaptureAndTag();

                if(result.Success) {
                    Console.CursorLeft = 0;
                    Console.WriteLine(result.Url);
                    Process.Start("explorer", result.Url);
                } else {
                    Console.WriteLine(":(");
                }
            }
        }

    }

    static ShazamResult CaptureAndTag() {
        using(var capture = new WasapiCapture()) {
            var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };
            var spectroFormat = new WaveFormat(Spectrogram.SAMPLE_RATE, 16, 1);

            capture.DataAvailable += (s, e) => {
                captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            capture.StartRecording();

            using(var resampler = new MediaFoundationResampler(captureBuf, spectroFormat)) {
                var spectro = new Spectrogram();
                var retryMs = 3000;
                var tagId = Guid.NewGuid().ToString();

                while(true) {
                    while(captureBuf.BufferedDuration.TotalSeconds < 1)
                        Thread.Sleep(100);

                    var segmentBuf = new byte[2 * Spectrogram.SAMPLES_IN_SEGMENT];
                    if(resampler.Read(segmentBuf, 0, segmentBuf.Length) != segmentBuf.Length)
                        throw new Exception();

                    var segmentSamples = new short[Spectrogram.SAMPLES_IN_SEGMENT];
                    for(var i = 0; i < Spectrogram.SAMPLES_IN_SEGMENT; i++)
                        segmentSamples[i] = BitConverter.ToInt16(segmentBuf, i * 2);

                    spectro.AddWaveSegment(segmentSamples);

                    if(spectro.SampleMs >= retryMs) {
                        //spectro.Draw().Save("c:/temp/spectro.png");

                        var sigBytes = Sig.Write(spectro.SampleCount, spectro.GetLandmarks());
                        var result = ShazamApi.SendRequest(tagId, spectro.SampleMs, sigBytes).GetAwaiter().GetResult();
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
