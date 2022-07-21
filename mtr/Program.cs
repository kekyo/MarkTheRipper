/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MarkTheRipper;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var help = false;
            var templatePath = default(string);
            var requiredBeforeCleanup = true;

            var options = new OptionSet()
            {
                { "template=", "Template html path", v => templatePath = v },
                { "no-cleanup", "Do not cleanup before processing if exists", _ => requiredBeforeCleanup = false },
                { "h|help", "Print this help", _ => help = true },
            };

            var extras = options.Parse(args);

            Console.Out.WriteLine($"MarkTheRipper [{ThisAssembly.AssemblyVersion}, {ThisAssembly.AssemblyMetadata.TargetFramework}]");

            if (help)
            {
                Console.Out.WriteLine("  Fantastic faster generates static site comes from simply Markdowns.");
                Console.Out.WriteLine("  Copyright (c) Kouji Matsui.");
                Console.Out.WriteLine("usage: mtr.exe [options] [<rendered dir path> [<contents dir path> ...]]");
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            else
            {
                Console.Out.WriteLine();

                var storeToBasePath = extras.
                    ElementAtOrDefault(0) ?? "docs";
                var contentsBasePathList = extras.
                    Skip(1).
                    DefaultIfEmpty("contents").
                    Distinct().
                    ToArray();
                var baseMetadata = new Dictionary<string, string>();

                await Driver.RunAsync(
                    Console.Out,
                    storeToBasePath,
                    templatePath,
                    baseMetadata, 
                    contentsBasePathList,
                    requiredBeforeCleanup,
                    default);
            }
        }
        catch (Exception ex)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(ex.ToString());
            return Marshal.GetHRForException(ex);
        }

        return 0;
    }
}
