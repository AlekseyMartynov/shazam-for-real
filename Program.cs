using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

class Program {

    static async Task Main(string[] args) {
        if(args.Length > 0) {
            if(args.ElementAtOrDefault(1) == "bisect") {
                using var bisect = new TagFileBisect(args[0]);
                await bisect.RunAsync();
            } else {
                await TagFile.RunAsync(args);
            }
        } else {
            await Interactive.RunAsync();
        }
    }

}
