using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project;

static class EternalSilence {
    readonly static ISampleProvider SILENCE = new SilenceProvider(ICaptureHelper.WAVE_FORMAT).ToSampleProvider();

    public static ISampleProvider AppendTo(ISampleProvider provider) {
        if(provider == null)
            return SILENCE;

        return new ConcatenatingSampleProvider([provider, SILENCE]);
    }

}
