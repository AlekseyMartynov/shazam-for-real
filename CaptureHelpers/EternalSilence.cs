using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;

class EternalSilence : ISampleProvider {
    readonly static ISampleProvider INSTANCE = new EternalSilence();

    public static ISampleProvider AppendTo(ISampleProvider provider) {
        if(provider == null)
            return INSTANCE;

        return new ConcatenatingSampleProvider(new[] { provider, INSTANCE });
    }

    private EternalSilence() {
    }

    public WaveFormat WaveFormat => ICaptureHelper.WAVE_FORMAT;

    public int Read(float[] buffer, int offset, int count) {
        Array.Fill(buffer, 0f, offset, count);
        return count;
    }
}
