public interface IReadableSensor
{
    bool TryGetLatestData(out ImuDataPoint dataPoint);
}