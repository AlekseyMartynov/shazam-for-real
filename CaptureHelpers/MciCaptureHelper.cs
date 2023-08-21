using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

class MciCaptureHelper : ICaptureHelper {
    static readonly object SYNC = new();
    static readonly int GENERATION_COUNT = 3;
    static readonly TimeSpan GENERATION_STEP = TimeSpan.FromSeconds(4);
    static readonly string TEMP_FILE_PATH = Path.Combine(Path.GetTempPath(), "shazam-for-real-tmp.wav");

    readonly WaveFormat Format;
    readonly bool[] GenerationRecording = new bool[GENERATION_COUNT];
    readonly IList<Stream> GenerationStreams = new List<Stream>();

    DateTime StartTime;
    Thread WorkerThread;
    bool StopRequested;

    public MciCaptureHelper(WaveFormat format) {
        Format = format;
        SampleProvider = new RawSourceWaveStream(Stream.Null, format).ToSampleProvider();
    }

    public void Dispose() {
        lock(SYNC) {
            StopRequested = true;
        }

        WorkerThread.Join();

        foreach(var s in GenerationStreams)
            s.Dispose();

        if(File.Exists(TEMP_FILE_PATH))
            File.Delete(TEMP_FILE_PATH);
    }

    public ISampleProvider SampleProvider { get; private set; }

    public void Start() {
        WorkerThread = new Thread(WorkerThreadProc);
        WorkerThread.Start();
    }

    void WorkerThreadProc() {
        lock(SYNC) {
            for(var i = 0; i < GENERATION_COUNT; i++) {
                var alias = GetAlias(i);
                MciSend("open new Type waveaudio Alias", alias);
                MciSend("set", alias,
                    "bitspersample", Format.BitsPerSample,
                    "channels", Format.Channels,
                    "samplespersec", Format.SampleRate,
                    "bytespersec", Format.AverageBytesPerSecond,
                    "alignment", Format.BlockAlign
                );
            }

            for(var i = 0; i < GENERATION_COUNT; i++) {
                MciSend("record", GetAlias(i));
                GenerationRecording[i] = true;
            }

            StartTime = DateTime.Now;
        }

        while(true) {
            lock(SYNC) {
                var allGenerationsStopped = true;

                for(var i = 0; i < GENERATION_COUNT; i++) {
                    if(GenerationRecording[i]) {
                        var willStop = StopRequested || DateTime.Now - StartTime > (1 + i) * GENERATION_STEP;

                        if(willStop) {
                            var alias = GetAlias(i);

                            if(!StopRequested) {
                                MciSend("save", alias, TEMP_FILE_PATH);
                                TempFileToSampleProvider();
                            }

                            MciSend("close", alias);
                            GenerationRecording[i] = false;
                        }
                    }

                    allGenerationsStopped = allGenerationsStopped && !GenerationRecording[i];
                }

                if(allGenerationsStopped) {
                    SampleProvider = new ConcatenatingSampleProvider(new[] {
                        SampleProvider,
                        new EternalSilence(Format)
                    });
                    return;
                }
            }

            Thread.Sleep(100);
        }
    }

    void TempFileToSampleProvider() {
        var stream = new MemoryStream(File.ReadAllBytes(TEMP_FILE_PATH));
        GenerationStreams.Add(stream);
        SampleProvider = new WaveFileReader(stream).ToSampleProvider();
    }

    static string GetAlias(int i) {
        return "rec" + i;
    }

    static string MciSend(params object[] command) {
        return MciSend(String.Join(" ", command));
    }

    static string MciSend(string command) {
        //Console.WriteLine(command);

        var buf = ArrayPool<char>.Shared.Rent(128);

        try {
            var code = mciSendString(command, buf, buf.Length, IntPtr.Zero);

            if(code != 0) {
                mciGetErrorString(code, buf, buf.Length);
                throw new Exception(BufToString(buf));
            }

            return BufToString(buf);
        } finally {
            ArrayPool<char>.Shared.Return(buf);
        }
    }

    static string BufToString(char[] buf) {
        var zIndex = Array.IndexOf(buf, '\0');

        if(zIndex > 0)
            return new String(buf, 0, zIndex);

        return new String(buf);
    }

    [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
    static extern uint mciSendString(string command, [Out] char[] returnBuf, int returnLen, IntPtr callbackHandle);

    [DllImport("winmm.dll", EntryPoint = "mciGetErrorStringW", CharSet = CharSet.Unicode)]
    static extern bool mciGetErrorString(uint errorCode, [Out] char[] returnBuf, int returnLen);

    class EternalSilence : ISampleProvider {
        public EternalSilence(WaveFormat format) {
            WaveFormat = format;
        }

        public WaveFormat WaveFormat { get; private set; }

        public int Read(float[] buffer, int offset, int count) {
            Array.Fill(buffer, 0f);
            return count;
        }
    }
}
