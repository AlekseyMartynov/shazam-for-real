using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program {

    static async Task Main(string[] args) {
        if(args.Length > 0) {
            await FileTag.RunAsync(args);
        } else {
            await Interactive.RunAsync();
        }
    }

}
