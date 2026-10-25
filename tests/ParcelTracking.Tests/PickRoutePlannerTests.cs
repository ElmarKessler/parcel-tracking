using ParcelTracking.Api.Routing;

namespace ParcelTracking.Tests;

public class PickRoutePlannerTests
{
    [Fact]
    public void SingleStop_WalksAroundWall()
    {
        string[] grid =
        [
            "S#A",
            ".#.",
            "...",
        ];

        var result = PickRoutePlanner.Plan(grid, [new PickStop(1, 'A')]);

        Assert.NotNull(result.Route);
        Assert.Equal(new[] { 1 }, result.Route.ParcelOrder);
        Assert.Equal(6, result.Route.Legs[0].Steps);  // down, along the bottom, up
        Assert.Equal(12, result.Route.TotalSteps);    // there and back
    }

    [Fact]
    public void WalledOffBin_IsReportedUnreachable()
    {
        string[] grid =
        [
            "S#A",
            "##.",
            "...",
        ];

        var result = PickRoutePlanner.Plan(grid, [new PickStop(1, 'A')]);

        Assert.Null(result.Route);
        Assert.Equal(new[] { 1 }, result.UnreachableParcelIds);
    }

    [Fact]
    public void BinMissingFromGrid_IsReportedUnreachable()
    {
        string[] grid = ["S.A"];

        var result = PickRoutePlanner.Plan(grid, [new PickStop(1, 'Z')]);

        Assert.Null(result.Route);
        Assert.Equal(new[] { 1 }, result.UnreachableParcelIds);
    }

    [Fact]
    public void Greedy_VisitsNearestStopFirst()
    {
        string[] grid = ["S.B..A"];

        var result = PickRoutePlanner.Plan(grid, [new PickStop(1, 'A'), new PickStop(2, 'B')]);

        Assert.NotNull(result.Route);
        Assert.Equal(new[] { 2, 1 }, result.Route.ParcelOrder); // B (2 steps) before A
        Assert.Equal(10, result.Route.TotalSteps);              // 2 + 3 + 5 back
    }

    [Fact]
    public void TwoParcelsInSameBin_SecondLegIsZero()
    {
        string[] grid = ["S.A"];

        var result = PickRoutePlanner.Plan(grid, [new PickStop(2, 'A'), new PickStop(1, 'A')]);

        Assert.NotNull(result.Route);
        Assert.Equal(new[] { 1, 2 }, result.Route.ParcelOrder); // tie: lower id first
        Assert.Equal(0, result.Route.Legs[1].Steps);
        Assert.Equal(4, result.Route.TotalSteps);
    }

    [Fact]
    public void NoStops_RouteIsEmpty()
    {
        string[] grid = ["S.A"];

        var result = PickRoutePlanner.Plan(grid, []);

        Assert.NotNull(result.Route);
        Assert.Empty(result.Route.ParcelOrder);
        Assert.Equal(0, result.Route.TotalSteps);
    }
}
