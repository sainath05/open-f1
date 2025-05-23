using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class ChampionshipStatsDisplay(
    ChampionshipPredictionProcessor championshipPrediction,
    TimingStatsProcessor timingStatsProcessor,
    DriverListProcessor driverList
) : IDisplay
{
    public Screen Screen => Screen.ChampionshipStats;

    public Task<IRenderable> GetContentAsync()
    {
        var layout = new Layout("Root").SplitColumns(
            new Layout("Left").SplitRows(
                new Layout("ChampionshipTable", GetTeamsChampionshipTable()) { Size = 15 },
                new Layout("SpeedTraps", GetSpeedTrapTable())
            ),
            new Layout("Right", GetDriversChampionshipTable()) { Size = 37 }
        );

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetDriversChampionshipTable()
    {
        var table = new Table();
        table.AddColumns(
            new TableColumn("Pos") { Width = 3, Alignment = Justify.Left },
            new TableColumn("Driver") { Alignment = Justify.Right },
            new TableColumn("Chg") { Width = 3, Alignment = Justify.Right },
            new TableColumn("Points") { Width = 6, Alignment = Justify.Right },
            new TableColumn("Gap") { Width = 3, Alignment = Justify.Right }
        );
        table.Expand();
        table.SimpleBorder();
        table.Title = new TableTitle("Drivers Championship");

        var drivers = championshipPrediction.Latest.Drivers.OrderBy(x => x.Value.PredictedPosition);
        var prevDriver = drivers.FirstOrDefault().Value;

        foreach (var (driverNumber, data) in drivers)
        {
            var driver = driverList.Latest.GetValueOrDefault(
                driverNumber,
                new DriverListDataPoint.Driver() { RacingNumber = driverNumber }
            );

            var relative = prevDriver.PredictedPoints - data.PredictedPoints;
            var change = data.PredictedPoints - data.CurrentPoints;
            var (color, indicator) = (data.PredictedPosition - data.CurrentPosition) switch
            {
                > 0 => (Color.Red, "▼"),
                < 0 => (Color.Green, "▲"),
                _ => (Color.White, string.Empty),
            };
            table.AddRow(
                new Text($"{data.PredictedPosition, 2}{indicator}", color),
                new Markup(DisplayUtils.MarkedUpDriverNumber(driver)),
                new Text($"+{change:N0}"),
                new Text($"{data.PredictedPoints.GetValueOrDefault(), 6:N0}"),
                new Text($"{-relative:N0}")
            );

            prevDriver = data;
        }

        return table;
    }

    private IRenderable GetTeamsChampionshipTable()
    {
        var table = new Table();
        table.AddColumns(
            new TableColumn("Pos") { Width = 4, Alignment = Justify.Left },
            new TableColumn("Team"),
            new TableColumn("Chg") { Width = 3, Alignment = Justify.Right },
            new TableColumn("Points") { Width = 6, Alignment = Justify.Right },
            new TableColumn("Gap") { Width = 4, Alignment = Justify.Right }
        );
        table.Expand();
        table.SimpleBorder();
        table.Title = new TableTitle("Constructors Championship");

        var teams = championshipPrediction.Latest.Teams.OrderBy(x => x.Value.PredictedPosition);
        var prevTeam = teams.FirstOrDefault().Value;

        foreach (var (teamName, data) in teams)
        {
            var driver = driverList
                .Latest.FirstOrDefault(x => x.Value.TeamName == data.TeamName)
                .Value;

            var relative = prevTeam.PredictedPoints - data.PredictedPoints;
            var change = data.PredictedPoints - data.CurrentPoints;

            table.AddRow(
                new Markup(
                    DisplayUtils.MarkedUpPositionIndicator(
                        data.PredictedPosition,
                        data.PredictedPosition - data.CurrentPosition
                    )
                ),
                new Markup($"[#{driver.TeamColour ?? "000000"} bold]{teamName}[/]"),
                new Text($"+{change:N0}"),
                new Text($"{data.PredictedPoints:N0}"),
                new Text($"{-relative:N0}")
            );

            prevTeam = data;
        }

        return table;
    }

    private IRenderable GetSpeedTrapTable()
    {
        var traps = new string[] { "FL", "I1", "I2", "ST" };
        var tables = new List<Table>();

        var stats = timingStatsProcessor.Latest.Lines;

        foreach (var trapKey in traps)
        {
            var table = new Table() { Title = new TableTitle(trapKey) };
            table.AddColumns("Driver", "Kph");
            var lines = stats.OrderBy(x => x.Value.BestSpeeds.GetValueOrDefault(trapKey)?.Position);
            foreach (var (driverNumber, line) in lines)
            {
                var driver = driverList.Latest.GetValueOrDefault(
                    driverNumber ?? "",
                    new() { RacingNumber = driverNumber }
                );
                var speed = line.BestSpeeds.GetValueOrDefault(trapKey)?.Value ?? "";
                if (!string.IsNullOrWhiteSpace(speed))
                {
                    table.AddRow(
                        new Markup(DisplayUtils.MarkedUpDriverNumber(driver)),
                        new Text(speed)
                    );
                }
            }

            table.SimpleBorder();

            tables.Add(table);
        }

        return new Columns(tables) { Expand = false };
    }
}
