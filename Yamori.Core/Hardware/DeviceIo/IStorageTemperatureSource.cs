namespace Yamori.Core.Hardware.DeviceIo;

/// <summary>
/// 物理ディスクの温度を、ディスクに直接問い合わせて読み取る。
/// </summary>
public interface IStorageTemperatureSource
{
    /// <summary>
    /// 物理ディスクの今の温度を読み取る。
    /// </summary>
    /// <param name="diskNumber">物理ディスクの番号（<c>\\.\PhysicalDriveN</c> の N）。</param>
    /// <returns>温度（℃）。ディスクやドライバーが対応していないなど、読めない場合は null。</returns>
    int? ReadCelsius(int diskNumber);
}
