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
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct)
    {
        var timezoneValue = metadata.Lookup("timezone") is { } timezoneExpression ?
            await reducer.ReduceExpressionAsync(timezoneExpression, metadata, ct) : null;
        if (timezoneValue != null)
        {
            if (timezoneValue is TimeZoneInfo tzi)
            {
                return dto => TimeZoneInfo.ConvertTime(dto, tzi);
            }

            var timezoneString = await MetadataUtilities.FormatValueAsync(
                timezoneValue, metadata, ct);

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
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        (await GetDateTimeConverterAsync(metadata, reducer, ct))(this.Date);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        InternalUtilities.NullAsync;

    public override string ToString() =>
        this.Date.ToString();

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        this.Date.ToString(format, formatProvider);
}
