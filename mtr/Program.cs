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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace MarkTheRipper;

public static class Program
{
    private static async Task ExtractSampleContentAsync(
        SafeDirectoryCreator dc, string resourceName, string storeToPath)
    {
        var basePath = Path.GetDirectoryName(storeToPath) switch
        {
            null => Path.DirectorySeparatorChar.ToString(),
            "" => ".",
            var dp => dp,
        };

        await dc.CreateIfNotExistAsync(basePath, default);

        using var ms = typeof(Program).Assembly.
            GetManifestResourceStream(resourceName);

        await BulkRipper.CopyContentToAsync(ms!, storeToPath, default);
    }

    private static async ValueTask ExtractSampleAsync(string sampleName)
    {
        var dc = new SafeDirectoryCreator();

        var baseName = "MarkTheRipper.embeds." + sampleName;
#if DEBUG
        foreach (var resourceName in
            typeof(Program).Assembly.GetManifestResourceNames().
            Where(resourceName => resourceName.StartsWith(baseName)))
        {
            var pathElements = resourceName.
                Substring(baseName.Length + 1).
                Split('.');
            var path = string.Join(
                Path.DirectorySeparatorChar.ToString(),
                pathElements.
                    Take(pathElements.Length - 3 - 1)) +
                    $".{pathElements.Last()}";
            await ExtractSampleContentAsync(
                dc, resourceName, path);
        }
#else
        await Task.WhenAll(
            typeof(Program).Assembly.GetManifestResourceNames().
            Where(resourceName => resourceName.StartsWith(baseName)).
            Select(resourceName => {
                var pathElements = resourceName.
                    Substring(baseName.Length + 1).
                    Split('.');
                var path = string.Join(
                    Path.DirectorySeparatorChar.ToString(),
                    pathElements.
                        Take(pathElements.Length - 3 - 1)) +
                        $".{pathElements.Last()}";
                return ExtractSampleContentAsync(
                    dc, resourceName, path);
            }));
#endif

        Console.Out.WriteLine($"Extracted sample files: {sampleName}");
        Console.Out.WriteLine();
    }

    private static string GetSafeStoreToPath(string? categoryArgument, string? fileNameArgument)
    {
        var categories = !string.IsNullOrWhiteSpace(categoryArgument) ?
            categoryArgument!.Split(new[] { '/', '\\', '-', '.', ',', ':', ';' }, StringSplitOptions.RemoveEmptyEntries) :
            Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(fileNameArgument))
        {
            var fileName = $"{fileNameArgument}.md";
            return Path.Combine(
                new[] { "contents" }.Concat(categories).Concat(new[] { fileName }).ToArray());
        }
        else
        {
            var suffix = 1;
            while (true)
            {
                var fileName = $"{DateTime.Now.ToString("yyyyMMdd")}{(suffix >= 2 ? $"-{suffix}" : "")}.md";
                var storeToPath = Path.Combine(
                    new[] { "contents" }.Concat(categories).Concat(new[] { fileName }).ToArray());
                if (!File.Exists(storeToPath))
                {
                    return storeToPath;
                }
                suffix++;
            }
        }
    }

    private static async ValueTask<string> ExtractNewMarkdownAsync(
        string storeToPath)
    {
        var dc = new SafeDirectoryCreator();

        await ExtractSampleContentAsync(
            dc,
            "MarkTheRipper.embeds.new.md",
            storeToPath);

        Console.Out.WriteLine($"Ready the new post: {storeToPath}");
        Console.Out.WriteLine();

        return storeToPath;
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var help = false;
            var resourceBasePath = "resources";
            var requiredBeforeCleanup = true;
            var requiredOpen = true;

            var options = new OptionSet()
            {
                { "resources=", "Resource base path", v => resourceBasePath = v },
                { "no-cleanup", "Do not cleanup before processing if exists", _ => requiredBeforeCleanup = false },
                { "n|no-open", "Do not open editor/browser automatically", _ => requiredOpen = true },
                { "h|help", "Print this help", _ => help = true },
            };

            var extras = options.Parse(args);

            Console.Out.WriteLine($"MarkTheRipper [{ThisAssembly.AssemblyVersion}, {ThisAssembly.AssemblyMetadata.TargetFramework}]");

            if (help)
            {
                Console.Out.WriteLine("  Fantastic faster generates static site comes from simply Markdowns.");
                Console.Out.WriteLine("  Copyright (c) Kouji Matsui.");
                Console.Out.WriteLine("  https://github.com/kekyo/MarkTheRipper");
                Console.Out.WriteLine();

                Console.Out.WriteLine("usage: mtr.exe [options] init [<sample name>]");
                Console.Out.WriteLine("  <sample name>: \"minimum\", \"standard\" and \"rich\"");
                Console.Out.WriteLine("usage: mtr.exe [options] new [<category path> [<slug>]]");
                Console.Out.WriteLine("usage: mtr.exe [options] [build [<store to dir path> [<contents dir path> ...]]]");
                Console.Out.WriteLine();

                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            else
            {
                Console.Out.WriteLine();

                var command = extras.
                    ElementAtOrDefault(0) ?? "build";
                switch (command)
                {
                    case "init":
                        var sampleName = extras.
                            ElementAtOrDefault(1) ?? "standard";
                        await ExtractSampleAsync(sampleName);
                        break;

                    case "new":
                        var storeToPath = GetSafeStoreToPath(
                            extras.ElementAtOrDefault(1),
                            extras.ElementAtOrDefault(2));
                        await ExtractNewMarkdownAsync(storeToPath);
                        if (requiredOpen)
                        {
                            Process.Start(storeToPath);
                        }
                        break;

                    case "build":
                        var storeToBasePath = extras.
                            ElementAtOrDefault(1) ?? "docs";
                        var contentsBasePathList = extras.
                            Skip(3).
                            DefaultIfEmpty("contents").
                            Distinct().
                            ToArray();
                        await Driver.RunAsync(
                            Console.Out,
                            storeToBasePath,
                            resourceBasePath,
                            contentsBasePathList,
                            requiredBeforeCleanup,
                            default);
                        if (requiredOpen)
                        {
                            var indexPath = Path.Combine(storeToBasePath, "index.html");
                            Process.Start(indexPath);
                        }
                        break;

                    default:
                        Console.Out.WriteLine($"Invalid command: {command}");
                        Console.Out.WriteLine();
                        break;
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
