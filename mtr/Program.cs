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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MarkTheRipper;

public static class Program
{
    private static async ValueTask ExtractSampleAsync(string sampleName)
    {
        var dc = new SafeDirectoryCreator();

        async Task ExtractSampleContentAsync(string resourceName)
        {
            var pathElements = resourceName.Split('.');
            var path = string.Join(
                Path.DirectorySeparatorChar.ToString(),
                pathElements.
                    Skip(3).   // "MarkTheRipper.embeds.minimum."
                    Take(pathElements.Length - 3 - 1)) +
                    $".{pathElements.Last()}";
            var basePath = Path.GetDirectoryName(path) switch
            {
                null => Path.DirectorySeparatorChar.ToString(),
                "" => ".",
                var dp => dp,
            };

            await dc.CreateIfNotExistAsync(basePath, default);

            using var ms = typeof(Program).Assembly.
                GetManifestResourceStream(resourceName);

            await Ripper.CopyContentToAsync(ms!, path, default);
        }

#if DEBUG
        foreach (var resourceName in
            typeof(Program).Assembly.GetManifestResourceNames().
            Where(resourceName => resourceName.StartsWith("MarkTheRipper.embeds." + sampleName)))
        {
            await ExtractSampleContentAsync(resourceName);
        }
#else
        await Task.WhenAll(
            typeof(Program).Assembly.GetManifestResourceNames().
            Where(resourceName => resourceName.StartsWith("MarkTheRipper.embeds." + sampleName)).
            Select(resourceName => ExtractSampleContentAsync(resourceName)));
#endif

        Console.Out.WriteLine($"Extracted sample files: {sampleName}");
        Console.Out.WriteLine();
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var help = false;
            var templateBasePath = "templates";
            var requiredBeforeCleanup = true;

            var options = new OptionSet()
            {
                { "templates=", "Template base path", v => templateBasePath = v },
                { "no-cleanup", "Do not cleanup before processing if exists", _ => requiredBeforeCleanup = false },
                { "h|help", "Print this help", _ => help = true },
            };

            var extras = options.Parse(args);

            Console.Out.WriteLine($"MarkTheRipper [{ThisAssembly.AssemblyVersion}, {ThisAssembly.AssemblyMetadata.TargetFramework}]");

            if (help)
            {
                Console.Out.WriteLine("  Fantastic faster generates static site comes from simply Markdowns.");
                Console.Out.WriteLine("  Copyright (c) Kouji Matsui.");
                Console.Out.WriteLine();

                Console.Out.WriteLine("usage: mtr.exe [options] new [<sample name>]");
                Console.Out.WriteLine("  <sample name>: \"minimum\", \"standard\" and \"rich\"");
                Console.Out.WriteLine("usage: mtr.exe [options] build [<store to dir path> [<contents dir path> ...]]");
                Console.Out.WriteLine();

                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            else
            {
                Console.Out.WriteLine();

                var command = extras.
                    ElementAtOrDefault(0) ?? "build";
                if (StringComparer.OrdinalIgnoreCase.Equals(command, "new"))
                {
                    var sampleName = extras.
                        ElementAtOrDefault(1) ?? "minimum";

                    await ExtractSampleAsync(sampleName);
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(command, "build"))
                {
                    var storeToBasePath = extras.
                        ElementAtOrDefault(1) ?? "docs";
                    var contentsBasePathList = extras.
                        Skip(3).
                        DefaultIfEmpty("contents").
                        Distinct().
                        ToArray();
                    var baseMetadata = new Dictionary<string, string>();

                    await Driver.RunAsync(
                        Console.Out,
                        storeToBasePath,
                        templateBasePath,
                        baseMetadata,
                        contentsBasePathList,
                        requiredBeforeCleanup,
                        default);
                }
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
