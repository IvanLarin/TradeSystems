﻿using MegaTrade.Common.Caching;
using MegaTrade.Common.Extensions;
using TSLab.DataSource;
using TSLab.Script;
using TSLab.Script.Handlers;

namespace MegaTrade.Basic.Gapping;

internal class AntiGap : IAntiGap
{
    public bool IsLastCandleOfSession => TradeStops[Now];

    public bool IsJustBeforeLastCandleOfSession => Now + 1 < TradeStops.Length && TradeStops[Now + 1];

    private bool[]? _tradeStops;

    public bool[] TradeStops => _tradeStops ??=
        Cache.Entry<bool[]>($"{nameof(AntiGap)}.{nameof(TradeStops)}", CacheKind.Memory, [Security.CacheKey()])
            .Calculate(() => Bars.Select((_, i) => IsStop(i)).ToArray());

    private bool IsStop(int i) => i == 0 && i <= FirstSessionStart || IsEndOfSession(i);

    private int? _firstSessionStart;

    private int FirstSessionStart => _firstSessionStart ??= GetFirstSessionStart();

    private int GetFirstSessionStart()
    {
        for (var i = 1; i < Bars.Count; i++)
            if (Bars[i].Date.Date > Bars[i - 1].Date.Date)
                return i;

        return 0;
    }

    private bool[]? _sessionEnds;

    private bool IsEndOfSession(int i) => (_sessionEnds ??= GetSessionEnds())[i];

    private bool[] GetSessionEnds()
    {
        var result = Context.GetOrCreateArray<bool>(Security.Bars.Count);

        for (var i = Bars.Count - 1; i >= 0; i--)
        {
            (DateTime dateTime, int previousEndIndex)? previousEnd = FindPreviousEnd(i);

            if (!previousEnd.HasValue)
            {
                result[i] = true;
                continue;
            }

            TimeSpan currentTime = Bars[i].Date.TimeOfDay;
            TimeSpan currentEnd = previousEnd.Value.dateTime.TimeOfDay;

            result[i] = currentEnd == currentTime;
        }

        return result;
    }

    private (DateTime time, int previousEndIndex)? FindPreviousEnd(int i)
    {
        for (var j = i - 1; j >= 0; j--)
            if (Bars[j].Date.Date < Bars[i].Date.Date)
                return (Bars[j].Date, j);
        return null;
    }

    private IReadOnlyList<IDataBar>? _bars;

    private IReadOnlyList<IDataBar> Bars => _bars ??= Security.Bars;

    public required ISecurity Security { private get; init; }

    public required IContext Context { private get; init; }

    public required INowProvider NowProvider { get; init; }

    private int Now => NowProvider.Now;
}