//#define ENABLE_DEBUG_CACHE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

sealed class TagFileBisect : IDisposable {
    const int RESOLUTION_SEC = 3;
    const int MAX_STEP = 1 << 6;

    readonly FileCaptureHelper CaptureHelper;
    readonly int GridSize;
    readonly int[] Grid;

    readonly Dictionary<int, string> Urls = [];

    public TagFileBisect(string filePath) {
#if ENABLE_DEBUG_CACHE
        DebugCache.EnsureDir(filePath);
#endif
        CaptureHelper = new(filePath);
        CaptureHelper.Start();

        GridSize = (int)(CaptureHelper.TotalTime.TotalSeconds / RESOLUTION_SEC);
        Grid = new int[GridSize];
        Array.Fill(Grid, -1);

        Urls[0] = "-";
    }

    public void Dispose() {
        CaptureHelper.Dispose();
    }

    public async Task RunAsync() {
        var tagCount = 0;

        for(var step = MAX_STEP; step > 0; step /= 2) {
            Console.WriteLine("Step: " + step);

            for(var gridIndex = 0; gridIndex < GridSize; gridIndex += step) {

                if(Grid[gridIndex] > -1) {
                    continue;
                }

                if(step < MAX_STEP) {
                    var prevIndex = gridIndex - step;
                    var nextIndex = gridIndex + step;
                    if(nextIndex < GridSize - 1) {
                        var prevID = Grid[prevIndex];
                        var nextID = Grid[nextIndex];
                        Debug.Assert(prevID > -1 && nextID > -1);
                        if(prevID == nextID) {
                            Grid[gridIndex] = prevID;
                            continue;
                        }
                    }
                }

                CaptureHelper.SeekTo(GridIndexToTime(gridIndex));

                ConsoleHelper.WriteTime(CaptureHelper.CurrentTime);

                var tag = await CachedTagAsync(gridIndex);

                var (id, url) = (0, Urls[0]);

                if(tag != null && tag.Success) {
                    id = int.Parse(tag.ID);
                    url = tag.Url;
                    Urls.TryAdd(id, url);
                }

                Console.WriteLine(url);

                Grid[gridIndex] = id;

                tagCount++;
            }
        }

        Console.WriteLine($"Tag/Grid ratio: {1d * tagCount / GridSize:P}");

        Denoise();

        Console.WriteLine("---");

        var prevId = -1;
        var dupSegmentTrace = new HashSet<int>();

        for(var gridIndex = 0; gridIndex < GridSize; gridIndex++) {
            var id = Grid[gridIndex];
            var url = Urls[id];
            ConsoleHelper.WriteTime(GridIndexToTime(gridIndex));
            Console.Write(url);
            if(id > 0 && id != prevId && !dupSegmentTrace.Add(id)) {
                Console.Write(" [DUP]");
            }
            Console.WriteLine();
            prevId = id;
        }
    }

    async Task<ShazamResult> CachedTagAsync(int gridIndex) {
#if ENABLE_DEBUG_CACHE
        if(DebugCache.TryLoad(gridIndex, out var cachedResult)) {
            return cachedResult;
        }
#endif
        var result = await CaptureAndTag.RunAsync(CaptureHelper);
#if ENABLE_DEBUG_CACHE
        DebugCache.Save(gridIndex, result);
#endif
        return result;
    }

    void Denoise() {
        for(var i = 1; i < GridSize - 1; i++) {
            var id = Grid[i];
            var prevID = Grid[i - 1];
            var nextID = Grid[i + 1];

            if(prevID == nextID && id != prevID) {
                Grid[i] = prevID;
                continue;
            }

            if(prevID != id && id != nextID) {
                Grid[i] = 0;
                continue;
            }
        }
    }

    static TimeSpan GridIndexToTime(int i) {
        return TimeSpan.FromSeconds(RESOLUTION_SEC * i);
    }

#if ENABLE_DEBUG_CACHE
    static class DebugCache {
        const string DIR = "c:/temp/shazam-bisect-cache";

        public static void EnsureDir(string inputFilePath) {
            if(Directory.Exists(DIR) && Directory.GetCreationTime(DIR) < File.GetLastWriteTime(inputFilePath)) {
                Directory.Delete(DIR, true);
            }
            if(!Directory.Exists(DIR)) {
                Directory.CreateDirectory(DIR);
            }
        }

        public static void Save(int gridIndex, ShazamResult result) {
            var lines = new List<string>();
            if(result != null) {
                lines.AddRange([
                    result.Success ? "1" : "0",
                result.ID,
                result.Url,
            ]);
            }
            File.WriteAllLines(GetCacheFilePath(gridIndex), lines);
        }

        public static bool TryLoad(int gridIndex, out ShazamResult result) {
            result = default;
            var filePath = GetCacheFilePath(gridIndex);
            if(File.Exists(filePath)) {
                var lines = File.ReadAllLines(filePath);
                if(lines.Length > 0) {
                    result = new() {
                        Success = lines[0] == "1",
                        ID = lines[1],
                        Url = lines[2],
                    };
                }
                return true;
            } else {
                return false;
            }
        }

        static string GetCacheFilePath(int gridIndex) {
            return Path.Join(DIR, gridIndex.ToString());
        }
    }
#endif

}
