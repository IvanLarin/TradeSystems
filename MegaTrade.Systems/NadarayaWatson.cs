﻿using MegaTrade.Basic;
using MegaTrade.Common.Extensions;
using MegaTrade.Indicators;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Handlers.Options;

namespace MegaTrade.Systems;

[HandlerName("НадараяВатсон")]
[InputsCount(2)]
[Input(0, TemplateTypes.SECURITY, Name = "Источник")]
[Input(1, TemplateTypes.SECURITY, Name = "Таймфрейм")]
[OutputsCount(0)]
public class NadarayaWatson : SystemBase
{
    public override bool IsLongEnterSignal =>
        NotInLongPosition && _closePrices[Now - 1] <= _nadarayaDown[Now - 1] && _closePrices[Now] > _nadarayaDown[Now];

    public override bool IsLongExitSignal =>
        _closePrices[Now - 1] >= _nadarayaUp[Now - 1] && _closePrices[Now] < _nadarayaUp[Now];

    public override bool IsShortEnterSignal => NotInShortPosition && _closePrices[Now - 1] >= _nadarayaUp[Now - 1] && _closePrices[Now] < _nadarayaUp[Now];

    public override bool IsShortExitSignal => _closePrices[Now - 1] <= _nadarayaDown[Now - 1] && _closePrices[Now] > _nadarayaDown[Now];

    public void Execute(ISecurity basicTimeframe, ISecurity timeframe)
    {
        _basicTimeframe = basicTimeframe;
        _timeframe = timeframe;
        _closePrices = basicTimeframe.ClosePrices;
        _nadarayaUp = timeframe.ClosePrices.NadarayaWatsonUp(Bandwidth, Multiplier, Range)
            .DecompressBy(timeframe);
        _nadarayaDown = timeframe.ClosePrices.NadarayaWatsonDown(Bandwidth, Multiplier, Range)
            .DecompressBy(timeframe);

        Run();
    }

    protected override Setup Setup() => new()
    {
        BasicTimeframe = _basicTimeframe,
        MinBarNumberLimits = [Range]
    };

    protected override void Draw() => Paint
        .Candles(_basicTimeframe)
        .Candles(_timeframe)
        .Function(_nadarayaUp, "Надарая-Ватсон")
        .Function(_nadarayaDown, "Надарая-Ватсон");

    private ISecurity _basicTimeframe = null!;
    private ISecurity _timeframe = null!;
    private IList<double> _nadarayaUp = [];
    private IList<double> _nadarayaDown = [];

    private IList<double> _closePrices = [];

    [HelperDescription("Range")]
    [HandlerParameter(Default = "500", Min = "2", Step = "1")]
    public int Range { get; set; }

    [HelperDescription("Multiplier")]
    [HandlerParameter(Default = "3", Min = "0.1", Step = "0.1")]
    public double Multiplier { get; set; }

    [HelperDescription("Bandwidth")]
    [HandlerParameter(Default = "8", Min = "0.1", Step = "0.1")]
    public double Bandwidth { get; set; }
}