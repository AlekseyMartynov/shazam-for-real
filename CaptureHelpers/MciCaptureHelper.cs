using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Project;

partial class MciCaptureHelper : ICaptureHelper {
    static readonly Lock SYNC = new();
    static readonly int GENERATION_COUNT = 3;
    static readonly TimeSpan GENERATION_STEP = TimeSpan.FromSeconds(4);

    readonly bool[] GenerationRecording = new bool[GENERATION_COUNT];
    readonly List<IDisposable> PendingDispose = [];

    DateTime StartTime;
    Thread WorkerThread;
    bool StopRequested;

    public MciCaptureHelper() {
        SampleProvider = new RawSourceWaveStream(Stream.Null, ICaptureHelper.WAVE_FORMAT).ToSampleProvider();
    }

    public void Dispose() {
        lock(SYNC) {
            StopRequested = true;
        }

        WorkerThread.Join();

        foreach(var disposable in PendingDispose)
            disposable.Dispose();
    }

    public bool Live => true;
    public ISampleProvider SampleProvider { get; private set; }
    public Exception Exception { get; private set; }

    public void Start() {
        WorkerThread = new Thread(WorkerThreadProc_Guarded);
        WorkerThread.Start();
    }

    void WorkerThreadProc_Guarded() {
        try {
            WorkerThreadProc();
        } catch(Exception x) {
            Exception = x;
        }
    }

    void WorkerThreadProc() {
        lock(SYNC) {
            for(var i = 0; i < GENERATION_COUNT; i++) {
                var alias = GetAlias(i);
                var format = ICaptureHelper.WAVE_FORMAT;
                MciSend("open new Type waveaudio Alias", alias);
                MciSend("set", alias,
                    "bitspersample", format.BitsPerSample,
                    "channels", format.Channels,
                    "samplespersec", format.SampleRate,
                    "bytespersec", format.AverageBytesPerSecond,
                    "alignment", format.BlockAlign
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
                        var fastStop = StopRequested || Exception != null;
                        var willStop = fastStop || DateTime.Now - StartTime > (1 + i) * GENERATION_STEP;

                        if(willStop) {
                            var alias = GetAlias(i);
                            var tempFilePath = default(string);

                            if(!fastStop) {
                                tempFilePath = Path.GetTempFileName();
                                try {
                                    MciSend("save", alias, tempFilePath);
                                    TempFileToSampleProvider(tempFilePath);
                                } catch(Exception x) {
                                    Exception ??= x;
                                }
                            }

                            MciSend("close", alias);
                            GenerationRecording[i] = false;

                            if(tempFilePath != default) {
                                File.Delete(tempFilePath);
                            }
                        }
                    }

                    allGenerationsStopped = allGenerationsStopped && !GenerationRecording[i];
                }

                if(allGenerationsStopped) {
                    SampleProvider = EternalSilence.AppendTo(SampleProvider);
                    return;
                }
            }

            Thread.Sleep(100);
        }
    }

    void TempFileToSampleProvider(string filePath) {
        // Buffer into memory because temp file will be deleted
        var stream = WithPendingDispose(new MemoryStream(File.ReadAllBytes(filePath)));
        var reader = WithPendingDispose(new WaveFileReader(stream));
        SampleProvider = reader.ToSampleProvider();
    }

    T WithPendingDispose<T>(T obj) where T : IDisposable {
        PendingDispose.Add(obj);
        return obj;
    }

    static string GetAlias(int i) {
        return "rec" + i;
    }

    static string MciSend(params ReadOnlySpan<object> command) {
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

    [LibraryImport("winmm", EntryPoint = "mciSendStringW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint mciSendString(string command, [Out] char[] returnBuf, int returnLen, IntPtr callbackHandle);

    [LibraryImport("winmm", EntryPoint = "mciGetErrorStringW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial void mciGetErrorString(uint errorCode, [Out] char[] returnBuf, int returnLen);
}
