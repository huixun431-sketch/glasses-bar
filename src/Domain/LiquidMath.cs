using System;

namespace GlassesBar.Domain;

public readonly record struct TransferResult(
    double SourceAfter,
    double DestinationAfter,
    double Transferred,
    double Spilled);

public static class LiquidMath
{
    public static TransferResult Transfer(
        double sourceAmount,
        double destinationAmount,
        double destinationCapacity,
        double requestedAmount)
    {
        sourceAmount = Math.Max(0d, sourceAmount);
        destinationAmount = Math.Max(0d, destinationAmount);
        destinationCapacity = Math.Max(0d, destinationCapacity);
        requestedAmount = Math.Max(0d, requestedAmount);

        var removed = Math.Min(sourceAmount, requestedAmount);
        var availableCapacity = Math.Max(0d, destinationCapacity - destinationAmount);
        var transferred = Math.Min(removed, availableCapacity);
        var spilled = Math.Max(0d, removed - transferred);

        return new TransferResult(
            sourceAmount - removed,
            destinationAmount + transferred,
            transferred,
            spilled);
    }

    public static double FlowFromTilt(double tilt01, double maxRatePerSecond, double deltaSeconds)
    {
        var clampedTilt = Math.Clamp(tilt01, 0d, 1d);
        return Math.Max(0d, maxRatePerSecond) * clampedTilt * Math.Max(0d, deltaSeconds);
    }
}

