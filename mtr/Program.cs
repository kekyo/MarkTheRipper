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
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var help = false;
            var templatePath = default(string);

            var options = new OptionSet()
            {
                { "template=", "Template html path", v => templatePath = v },
                { "h|help", "Print this help", _ => help = true },
            };

            var extras = options.Parse(args);

            Console.Out.WriteLine($"MarkTheRipper [{ThisAssembly.AssemblyVersion}]");
            Console.Out.WriteLine("  Fantastic faster generates static site comes from simply Markdowns.");
            Console.Out.WriteLine("  Copyright (c) Kouji Matsui.");

            if (help)
            {
                Console.Out.WriteLine("usage: mtr.exe [options] [<rendered dir path> [<contents dir path> ...]]");
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            else
            {
                Console.Out.WriteLine();

                var template = templatePath != null ?
                    File.ReadAllText(templatePath) :
                    new StreamReader(typeof(Program).Assembly.GetManifestResourceStream("MarkTheRipper.template.html")!).ReadToEnd();

                var storeToBasePath = extras.
                    ElementAtOrDefault(0) ?? "docs";
                var contentsBasePathList = extras.
                    Skip(1).
                    DefaultIfEmpty("contents").
                    ToArray();
                var baseMetadata = new Dictionary<string, string>();

                var generator = new Ripper(
                    storeToBasePath, template, baseMetadata);
                var count = await generator.RipOffAsync(
                    (relativePath, basePath) =>
                    {
                        Console.WriteLine($"Generated: {Path.Combine(basePath, relativePath)}");
                        return default;
                    },
                    contentsBasePathList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Marshal.GetHRForException(ex);
        }

        return 0;
    }
}
