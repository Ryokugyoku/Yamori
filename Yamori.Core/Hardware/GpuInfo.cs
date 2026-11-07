namespace Yamori.Core.Hardware;

/// <summary>
/// GPU（ビデオアダプター）1 枚の情報。
/// </summary>
/// <param name="Name">製品名。</param>
/// <param name="DedicatedMemoryBytes">専用ビデオメモリ（バイト）。取得できない場合は null。</param>
/// <param name="DriverVersion">ドライバーのバージョン。</param>
public sealed record GpuInfo(string Name, ulong? DedicatedMemoryBytes, string DriverVersion)
{
    /// <summary>
    /// <see cref="DedicatedMemoryBytes"/> が WMI の値で、4GB を上限に切り詰められている可能性があるかどうか。
    /// DirectX（DXCore）から正確な値を取れた場合は false。
    /// </summary>
    public bool IsDedicatedMemoryCapped { get; init; }

    /// <summary>GPU が使える共有メモリ（メインメモリから借りる分、バイト）。取得できない場合は null。</summary>
    public ulong? SharedMemoryBytes { get; init; }

    /// <summary>CPU に内蔵された GPU かどうか。判別できない場合は null。</summary>
    public bool? IsIntegrated { get; init; }

    /// <summary>メーカー（例：NVIDIA、Intel Corporation）。</summary>
    public string? Vendor { get; init; }

    /// <summary>ドライバーの日付。</summary>
    public DateOnly? DriverDate { get; init; }

    /// <summary>現在の画面の横の解像度（ピクセル）。画面をつないでいない場合は null。</summary>
    public int? HorizontalResolution { get; init; }

    /// <summary>現在の画面の縦の解像度（ピクセル）。画面をつないでいない場合は null。</summary>
    public int? VerticalResolution { get; init; }

    /// <summary>現在のリフレッシュレート（Hz）。画面をつないでいない場合は null。</summary>
    public int? RefreshRateHz { get; init; }
}
