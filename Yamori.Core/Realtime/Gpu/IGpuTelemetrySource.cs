namespace Yamori.Core.Realtime.Gpu;

/// <summary>
/// GPU のセンサーの値（温度、ファン、電力、クロック）を読み取る。
/// </summary>
public interface IGpuTelemetrySource
{
    /// <summary>
    /// ハードウェアの GPU ごとに、今のセンサーの値を読み取る。ソフトウェアで描画するアダプターは含めない。
    /// </summary>
    /// <returns>GPU ごとの値。読めない場合は空。</returns>
    IReadOnlyList<GpuTelemetry> Read();
}
