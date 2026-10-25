using ParcelTracking.Api.Domain;

namespace ParcelTracking.Tests;

public class ParcelStateMachineTests
{
    [Theory]
    [InlineData(ParcelStatus.Created, ParcelStatus.Picked)]
    [InlineData(ParcelStatus.Picked, ParcelStatus.Packed)]
    [InlineData(ParcelStatus.Packed, ParcelStatus.Shipped)]
    [InlineData(ParcelStatus.Shipped, ParcelStatus.Delivered)]
    public void OneStepForward_IsAllowed(ParcelStatus from, ParcelStatus to)
    {
        Assert.True(ParcelStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ParcelStatus.Created, ParcelStatus.Packed)]     // skips a step
    [InlineData(ParcelStatus.Picked, ParcelStatus.Created)]     // backwards
    [InlineData(ParcelStatus.Picked, ParcelStatus.Picked)]      // same status
    [InlineData(ParcelStatus.Delivered, ParcelStatus.Created)]  // after delivery
    [InlineData(ParcelStatus.Created, ParcelStatus.Delivered)]  // jumps to the end
    public void OtherTransitions_AreRejected(ParcelStatus from, ParcelStatus to)
    {
        Assert.False(ParcelStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void AllPairs_ExactlyFourAreAllowed()
    {
        var all = Enum.GetValues<ParcelStatus>();

        var allowedCount = all
            .SelectMany(from => all, (from, to) => (from, to))
            .Count(pair => ParcelStateMachine.CanTransition(pair.from, pair.to));

        Assert.Equal(4, allowedCount);
    }
}
