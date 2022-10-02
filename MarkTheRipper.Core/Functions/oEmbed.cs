////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Functions.Internal;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

// Thanks suggest Ovis oEmbed handling:
// https://github.com/Ovis/BlogGenerator/blob/main/BlogGenerator/ShortCodes/OEmbedShortCodes.cs

internal static class oEmbed
{
    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression> Internal_oEmbedAsync(
        Uri permaLink,
        MetadataContext metadata,
        bool embedPageIfAvailable,
        CancellationToken ct)
    {
        var httpAccessor = (await metadata.LookupValueAsync(
            "httpAccessor", HttpAccessor.Instance, ct).
            ConfigureAwait(false))!;

        var mc = metadata.Spawn();
        mc.SetValue("permaLink", permaLink);

        //////////////////////////////////////////////////////////////////
        // Step 1. Automatic resolve using global oEmbed provider list.

        // Render oEmbed from perma link.
        if (await oEmbedRenderrer.Render_oEmbedAsync(
            httpAccessor, mc, permaLink, embedPageIfAvailable, ct).
            ConfigureAwait(false) is { } result1)
        {
            // Done.
            return result1;
        }

        //////////////////////////////////////////////////////////////////
        // Step 2. Fetch HTML from perma link directly.

        try
        {
            // TODO: cache system
            var html = await httpAccessor.FetchHtmlAsync(
                permaLink, ct).
                ConfigureAwait(false);

            //////////////////////////////////////////////////////////////////
            // Step 3. Resolve by oEmbed discover meta tag link.

            // Contains oEmbed meta tags.
            if (html.Head?.QuerySelector("link[type='application/json+oembed']") is { } oEmbedLinkElement &&
                oEmbedLinkElement.GetAttribute("href") is { } hrefString &&
                hrefString.Trim() is { } hs &&
                Uri.TryCreate(hs, UriKind.Absolute, out var href))
            {
                // Render oEmbed from discovered perma link.
                if (await oEmbedRenderrer.Render_oEmbedDiscoveryAsync(
                    httpAccessor, mc, href, embedPageIfAvailable, ct).
                    ConfigureAwait(false) is { } result2)
                {
                    // Done.
                    return result2;
                }
            }

            //////////////////////////////////////////////////////////////////
            // Step 4. Give up oEmbed resolving, retreive meta tags from HTML.

            var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
                html, permaLink);
            oEmbedUtilities.SetHtmlMetadata(mc, htmlMetadata);

            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-card-{siteName}.html` ==> `layout-oEmbed-card.html`
            var layoutNode = await metadata.Get_oEmbedLayoutAsync(
                htmlMetadata, "card", ct).
                ConfigureAwait(false);

            // Render with layout AST with overall metadata.
            var overallHtmlContent = await layoutNode.RenderOverallAsync(mc, ct).
                ConfigureAwait(false);

            // Done.
            return new ValueExpression(
                new HtmlContentEntry(overallHtmlContent));
        }
        catch (Exception ex)
        {
            Trace.WriteLine(
                $"Could not fetch perma link content: Url={permaLink}, Message={ex.Message}");
        }

        //////////////////////////////////////////////////////////////////
        // Step 6. Could not fetch any information.

        {
            // Render with layout.
            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-card.html`
            var layoutNode = await mc.Get_oEmbedLayoutAsync(
                "card", ct).
                ConfigureAwait(false);

            // Render with layout AST with overall metadata.
            var overallHtmlContent = await layoutNode.RenderOverallAsync(mc, ct).
                ConfigureAwait(false);

            // Done.
            return new ValueExpression(
                new HtmlContentEntry(overallHtmlContent));
        }
    }

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<IExpression> EmbedAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid embed function arguments: Count={parameters.Length}");
        }

        var permaLinkString = (await parameters[0].
            ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false)).
            Trim();
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid embed function argument: URL={permaLinkString}");
        }

        return await Internal_oEmbedAsync(
            permaLink, metadata, true, ct).
            ConfigureAwait(false);
    }

    public static async ValueTask<IExpression> CardAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid card function arguments: Count={parameters.Length}");
        }

        var permaLinkString = (await parameters[0].
            ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false)).
            Trim();
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid card function argument: URL={permaLinkString}");
        }

        return await Internal_oEmbedAsync(
            permaLink, metadata, false, ct).
            ConfigureAwait(false);
    }
}
