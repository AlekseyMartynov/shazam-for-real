using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

class Program {

    static async Task<int> Main(string[] args) {
#if DEBUG
        if(IsTestRun(args)) {
            return await Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions);
        }
#endif
        if(args.Length > 0) {
            await TagFile.RunAsync(args);
        } else {
            await Interactive.RunAsync();
        }
        return default;
    }

    static bool IsTestRun(string[] args) {
        // https://github.com/microsoft/testfx/tree/main/docs/mstest-runner-protocol
        return args.Contains("--server");
    }
}
