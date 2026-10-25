using System.Diagnostics.CodeAnalysis;

namespace ParcelTracking.Api.Routing;

// Grid legend: '.' free, '#' shelf or wall, 'S' start (dock), 'A'-'Z' bins.
public static class WarehouseLayouts
{
    private static readonly Dictionary<int, string[]> Layouts = new()
    {
        [1] =
        [
            "S.........",
            ".##A.##B..",
            ".##..##...",
            "..........",
            ".##C.##D..",
            ".##..##..E",
            "..........",
        ],
    };

    public static bool TryGet(int warehouseId, [NotNullWhen(true)] out string[]? grid) =>
        Layouts.TryGetValue(warehouseId, out grid);

    public static bool HasBin(string[] grid, char bin) =>
        bin is >= 'A' and <= 'Z' && grid.Any(row => row.Contains(bin));
}
