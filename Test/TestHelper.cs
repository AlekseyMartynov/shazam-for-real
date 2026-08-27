#if DEBUG
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project.Test;

static class TestHelper {
    public static readonly string DATA_DIR;

    static TestHelper() {
        DATA_DIR = Path.Combine(
            Path.GetDirectoryName(typeof(TestHelper).Assembly.Location),
            "../../../TestData"
        );
    }
}
#endif
