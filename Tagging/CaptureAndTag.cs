using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Project;

static class CaptureAndTag {
    static readonly float[] CHUNK = new float[Analysis.CHUNK_SIZE];

    public static async Task<ShazamResult> RunAsync(CaptureHelper captureHelper) {
        var analysis = new Analysis();
        var finder = new PeakFinder(analysis);

        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        while(true) {
            var readChunkResult = await ReadChunkAsync(captureHelper);

            if(readChunkResult == ReadChunkResult.EOF)
                return null;

            var isTimeout = readChunkResult == ReadChunkResult.Timeout;

            if(!isTimeout) {
                analysis.AddChunk(CHUNK);
            }

            if(analysis.ProcessedMs >= retryMs || isTimeout) {
                //new Painter(analysis, finder).Paint("c:/temp/spectro.png");
                //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");

                var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                var result = await ShazamApi.SendRequestAsync(tagId, analysis.ProcessedMs, sigBytes);
                if(result.Success || isTimeout)
                    return result;

                retryMs = result.RetryMs;
                if(retryMs == 0)
                    return result;
            }
        }
    }

    static async Task<ReadChunkResult> ReadChunkAsync(CaptureHelper captureHelper) {
        var sampleProvider = captureHelper.SampleProvider;
        var offset = 0;
        var expectedCount = CHUNK.Length;
        var lastDataTick = Environment.TickCount64;

        while(true) {
            if(captureHelper.Exception != null)
                ExceptionDispatchInfo.Capture(captureHelper.Exception).Throw();

            var actualCount = sampleProvider.Read(CHUNK.AsSpan(offset, expectedCount));

            if(actualCount == expectedCount)
                return ReadChunkResult.OK;

            if(!captureHelper.Live)
                return ReadChunkResult.EOF;

            if(actualCount > 0) {
                lastDataTick = Environment.TickCount64;
            } else if(Environment.TickCount64 - lastDataTick > 5000) {
                // Added primarily for WASAPI Loopback
                // which only receives data when something is actually playing
                // https://github.com/PortAudio/portaudio/issues/935
                return ReadChunkResult.Timeout;
            }

            offset += actualCount;
            expectedCount -= actualCount;

            await Task.Delay(100);
        }
    }

    enum ReadChunkResult {
        OK,
        EOF,
        Timeout
    }
}
