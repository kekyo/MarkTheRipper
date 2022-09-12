/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Html.Dom;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO
{
    public interface IHttpAccessor
    {
        ValueTask<JToken> FetchJsonAsync(Uri url, CancellationToken ct);
        ValueTask<IHtmlDocument> FetchHtmlAsync(Uri url, CancellationToken ct);
    }
}
