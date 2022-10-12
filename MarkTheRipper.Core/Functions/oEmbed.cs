﻿////////////////////////////////////////////////////////////////////////////////////
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

    private static async ValueTask<IExpression> ProcessPermaLinkAsync(
        MetadataContext metadata,
        Uri permaLink,
        bool embedPageIfAvailable,
        CancellationToken ct)
    {
        var httpAccessor = await metadata.LookupValueAsync<IHttpAccessor?>(
            "httpAccessor", null, ct).
            ConfigureAwait(false);
        if (httpAccessor == null)
        {
            throw new InvalidOperationException(
                "Could not find any HTTP accessor.");
        }

        var mc = metadata.Spawn();
        mc.SetValue("permaLink", permaLink);

        //////////////////////////////////////////////////////////////////
        // Step 1. Is it in amazon product page URL?

        if (await AmazonRenderrer.RenderAmazonHtmlContentAsync(
            httpAccessor, mc, permaLink, embedPageIfAvailable, ct).
            ConfigureAwait(false) is { } amazonHtmlContent)
        {
            // Accept with Amazon HTML.
            return amazonHtmlContent;
        }

        //////////////////////////////////////////////////////////////////
        // Step 2. Automatic resolve using global oEmbed provider list.

        // Render oEmbed from perma link.
        if (await oEmbedRenderrer.Render_oEmbedAsync(
            httpAccessor, mc, permaLink, embedPageIfAvailable, ct).
            ConfigureAwait(false) is { } result1)
        {
            // Done.
            return result1;
        }

        //////////////////////////////////////////////////////////////////
        // Step 3. Fetch HTML from perma link directly.

        try
        {
            // TODO: cache system
            var html = await httpAccessor.FetchHtmlAsync(
                permaLink, ct).
                ConfigureAwait(false);

            //////////////////////////////////////////////////////////////////
            // Step 4. Resolve by oEmbed discover meta tag link.

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
            // Step 5. Give up oEmbed resolving, retreive meta tags from HTML.

            var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
                html, permaLink);

            // Removed parent content body.
            mc.SetValue("contentBody", string.Empty);

            return await oEmbedRenderrer.RenderWithHtmlMetadataAsync(
                mc, "card", htmlMetadata, ct).
                ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(
                $"Could not fetch perma link content: Url={permaLink}, Message={ex.Message}");
        }

        //////////////////////////////////////////////////////////////////
        // Step 6. Could not fetch any information.

        {
            // Removed parent content body.
            mc.SetValue("contentBody", string.Empty);

            // Render with layout.
            // Get layout AST (ITextTreeNode).
            // `card.html`
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

        return await ProcessPermaLinkAsync(
            metadata, permaLink, true, ct).
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

        return await ProcessPermaLinkAsync(
            metadata, permaLink, false, ct).
            ConfigureAwait(false);
    }
}
