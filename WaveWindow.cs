using System;
using System.Collections.Generic;

class WaveWindow {
    readonly int
        SampleRate,
        ChunkSize,
        ChunkCount;

    readonly Queue<IReadOnlyCollection<short>> Chunks;

    public WaveWindow(int sampleRate, int chunkSize, int chunkCount) {
        SampleRate = sampleRate;
        ChunkSize = chunkSize;
        ChunkCount = chunkCount;
        Chunks = new Queue<IReadOnlyCollection<short>>(ChunkCount);
    }

    public bool IsFull => Chunks.Count == ChunkCount;

    public int ProcessedSamples { get; private set; }
    public int ProcessedMs => ProcessedSamples * 1000 / SampleRate;

    public void AddChunk(IReadOnlyCollection<short> samples) {
        if(samples.Count != ChunkSize)
            throw new ArgumentException();

        ProcessedSamples += ChunkSize;

        if(IsFull)
            Chunks.Dequeue();

        Chunks.Enqueue(samples);
    }

    public IReadOnlyList<short> GetSamples() {
        var result = new short[ChunkCount * ChunkSize];
        var i = 0;

        foreach(var chunk in Chunks) {
            foreach(var sample in chunk) {
                result[i] = sample;
                i++;
            }
        }

        return result;
    }
}
