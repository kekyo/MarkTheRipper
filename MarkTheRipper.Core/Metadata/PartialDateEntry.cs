/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

#if !NET6_0_OR_GREATER
using TimeZoneConverter;
#endif

namespace MarkTheRipper.Metadata;

internal sealed class PartialDateEntry :
    IMetadataEntry, IFormattable
{
    public readonly DateTimeOffset Date;

    public PartialDateEntry(DateTimeOffset date) =>
        this.Date = date;

    private static async ValueTask<Func<DateTimeOffset, DateTimeOffset>> GetDateTimeConverterAsync(
        MetadataContext metadata,
        CancellationToken ct)
    {
        var timezoneValue = metadata.Lookup("timezone") is { } timezoneExpression ?
            await timezoneExpression.ReduceExpressionAsync(metadata, ct).
                ConfigureAwait(false) : null;
        if (timezoneValue != null)
        {
            if (timezoneValue is TimeZoneInfo tzi)
            {
                return dto => TimeZoneInfo.ConvertTime(dto, tzi);
            }

            var timezoneString = await MetadataUtilities.FormatValueAsync(
                timezoneValue, metadata, ct).
                ConfigureAwait(false);

            if (TimeSpan.TryParse(timezoneString, out var timezoneDifference))
            {
                return dto => dto.Add(timezoneDifference);
            }

#if NET6_0_OR_GREATER
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timezoneString, out var windowsId))
            {
                tzi = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                return dto => TimeZoneInfo.ConvertTime(dto, tzi);
            }
            else
            {
                try
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById(timezoneString);
                    return dto => TimeZoneInfo.ConvertTime(dto, tzi);
                }
                catch
                {
                }
            }
#else
            if (TZConvert.TryGetTimeZoneInfo(timezoneString, out tzi))
            {
                return dto => TimeZoneInfo.ConvertTime(dto, tzi);
            }
#endif
        }

        return date => date;
    }

    public async ValueTask<object?> GetImplicitValueAsync(
        MetadataContext metadata, CancellationToken ct) =>
        (await GetDateTimeConverterAsync(metadata, ct).ConfigureAwait(false))(this.Date);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        Utilities.NullAsync;

    public override string ToString() =>
        this.Date.ToString();

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        this.Date.ToString(format, formatProvider);
}
