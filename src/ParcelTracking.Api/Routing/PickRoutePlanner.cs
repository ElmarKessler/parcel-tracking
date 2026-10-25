namespace ParcelTracking.Api.Routing;

public record PickStop(int ParcelId, char Bin);
public record RouteLeg(string Destination, int Steps);
public record PickRoute(IReadOnlyList<int> ParcelOrder, IReadOnlyList<RouteLeg> Legs, int TotalSteps);
public record PickRouteResult(PickRoute? Route, IReadOnlyList<int> UnreachableParcelIds);

// Shortest path between two cells: BFS (every step costs 1).
// Order of stops: greedy nearest neighbour. Simple, not guaranteed optimal (optimal = TSP).
public static class PickRoutePlanner
{
    private static readonly (int Dr, int Dc)[] Directions = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    public static PickRouteResult Plan(string[] grid, IReadOnlyList<PickStop> stops)
    {
        if (grid.Length == 0 || grid.Any(row => row.Length != grid[0].Length))
            throw new ArgumentException("Grid must be non-empty and rectangular.", nameof(grid));

        var start = Find(grid, 'S') ?? throw new ArgumentException("Grid has no start cell 'S'.", nameof(grid));

        // Resolve each parcel's bin to a cell and check it can be reached at all.
        var distFromStart = Distances(grid, start);
        var targets = new Dictionary<int, (int R, int C)>();
        var bins = new Dictionary<int, char>();
        var unreachable = new List<int>();

        foreach (var stop in stops)
        {
            var cell = Find(grid, stop.Bin);
            if (cell is null || distFromStart[cell.Value.R, cell.Value.C] < 0)
            {
                unreachable.Add(stop.ParcelId);
                continue;
            }
            targets[stop.ParcelId] = cell.Value;
            bins[stop.ParcelId] = stop.Bin;
        }

        if (unreachable.Count > 0)
            return new PickRouteResult(null, unreachable);

        var order = new List<int>();
        var legs = new List<RouteLeg>();
        var remaining = targets.Keys.OrderBy(id => id).ToList(); // sorted: ties go to the lower parcel id
        var current = start;

        while (remaining.Count > 0)
        {
            var dist = Distances(grid, current); // one BFS gives distances to all cells
            var next = remaining.MinBy(id => dist[targets[id].R, targets[id].C]);
            var steps = dist[targets[next].R, targets[next].C];

            order.Add(next);
            legs.Add(new RouteLeg($"parcel {next} (bin {bins[next]})", steps));
            current = targets[next];
            remaining.Remove(next);
        }

        var backToStart = Distances(grid, current)[start.R, start.C];
        legs.Add(new RouteLeg("start", backToStart));

        return new PickRouteResult(new PickRoute(order, legs, legs.Sum(l => l.Steps)), []);
    }

    // BFS from one cell. Result: steps to every cell, -1 = not reachable.
    private static int[,] Distances(string[] grid, (int R, int C) from)
    {
        int rows = grid.Length, cols = grid[0].Length;
        var dist = new int[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                dist[r, c] = -1;

        var queue = new Queue<(int R, int C)>();
        dist[from.R, from.C] = 0;
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            foreach (var (dr, dc) in Directions)
            {
                int nr = r + dr, nc = c + dc;
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols)
                    continue;
                if (grid[nr][nc] == '#' || dist[nr, nc] != -1)
                    continue;

                dist[nr, nc] = dist[r, c] + 1;
                queue.Enqueue((nr, nc));
            }
        }

        return dist;
    }

    private static (int R, int C)? Find(string[] grid, char symbol)
    {
        for (var r = 0; r < grid.Length; r++)
        {
            var c = grid[r].IndexOf(symbol);
            if (c >= 0)
                return (r, c);
        }
        return null;
    }
}
