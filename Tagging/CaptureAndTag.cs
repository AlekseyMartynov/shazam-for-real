using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

static class CaptureAndTag {
    static readonly float[] CHUNK = new float[Analysis.CHUNK_SIZE];

    public static async Task<ShazamResult> RunAsync(ICaptureHelper captureHelper) {
        var analysis = new Analysis();
        var finder = new PeakFinder(analysis);

        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        while(true) {
            var readChunkResult = await ReadChunkAsync(captureHelper);

            if(readChunkResult == ReadChunkResult.EOF)
                return null;

            if(readChunkResult == ReadChunkResult.SampleProviderChanged) {
                analysis = new Analysis();
                finder = new PeakFinder(analysis);
                continue;
            }

            analysis.AddChunk(CHUNK);

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

    static async Task<ReadChunkResult> ReadChunkAsync(ICaptureHelper captureHelper) {
        var sampleProvider = captureHelper.SampleProvider;
        var offset = 0;
        var expectedCount = CHUNK.Length;

        while(true) {
            if(captureHelper.Exception != null)
                ExceptionDispatchInfo.Capture(captureHelper.Exception).Throw();

            if(captureHelper.SampleProvider != sampleProvider)
                return ReadChunkResult.SampleProviderChanged;

            var actualCount = sampleProvider.Read(CHUNK, offset, expectedCount);

            if(actualCount == expectedCount)
                return ReadChunkResult.OK;

            if(!captureHelper.Live)
                return ReadChunkResult.EOF;

            offset += actualCount;
            expectedCount -= actualCount;

            await Task.Delay(100);
        }
    }

    enum ReadChunkResult {
        OK,
        SampleProviderChanged,
        EOF
    }
}
