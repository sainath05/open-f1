using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingHistoryDisplay(
    State state,
    TimingDataProcessor timingData,
    DriverListProcessor driverList
) : IDisplay
{
    public Screen Screen => Screen.TimingHistory;

    private Style _personalBest = new(background: Color.Green);
    private Style _overallBest = new(background: Color.Purple);
    private Style _normal = new();

    public Task<IRenderable> GetContentAsync()
    {
        var timingTower = GetTimingTower();

        var layout = new Layout("Root").SplitRows(
            new Layout("Timing Tower", timingTower)
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetTimingTower()
    {
        var table = new Table();
        table.AddColumns("", "Gap", "Interval", "Last Lap", "S1", "S2", "S3");
        var drivers = timingData.DriversByLap.GetValueOrDefault(state.CursorOffset);
        if (drivers is null)
        {
            return new Text($"No Data for Lap {state.CursorOffset}");
        }

        foreach (var (driverNumber, line) in drivers.OrderBy(x => x.Value.Line))
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();

            table.AddRow(
                new Text($"{line.Position, 2} {driver.RacingNumber, 2} {driver.Tla}"),
                new Text(line.GapToLeader ?? ""),
                new Text(line.IntervalToPositionAhead?.Value ?? ""),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                )
            );
        }

        table.NoBorder();

        return table;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return _normal;
        if (time.OverallFastest ?? false)
            return _overallBest;
        if (time.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }
}