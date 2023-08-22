using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

static class CaptureAndTag {
    static readonly float[] CHUNK = new float[Analysis.CHUNK_SIZE];

    public static async Task<ShazamResult> RunAsync(ICaptureHelper captureHelper, int initialDurationMs) {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        var retryMs = initialDurationMs;
        var tagId = Guid.NewGuid().ToString();

        while(true) {
            var readChunkResult = ReadChunk(captureHelper);

            if(readChunkResult == ReadChunkResult.SampleProviderChanged) {
                analysis = new Analysis();
                finder = new LandmarkFinder(analysis);
                continue;
            }

            analysis.AddChunk(CHUNK);

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

    static ReadChunkResult ReadChunk(ICaptureHelper captureHelper) {
        var sampleProvider = captureHelper.SampleProvider;
        var offset = 0;
        var expectedCount = CHUNK.Length;

        while(true) {
            if(captureHelper.SampleProvider != sampleProvider)
                return ReadChunkResult.SampleProviderChanged;

            var actualCount = sampleProvider.Read(CHUNK, offset, expectedCount);

            if(actualCount == expectedCount)
                return ReadChunkResult.OK;

            offset += actualCount;
            expectedCount -= actualCount;

            Thread.Sleep(100);
        }
    }

    enum ReadChunkResult {
        OK,
        SampleProviderChanged
    }
}
