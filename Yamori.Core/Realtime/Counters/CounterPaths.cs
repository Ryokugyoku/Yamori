namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// リアルタイム表示で読むパフォーマンスカウンターの英語名のパス。
/// </summary>
public static class CounterPaths
{
    /// <summary>論理プロセッサごとの使用率（%）。インスタンスは <c>0,0</c> のような「グループ,番号」と、<c>_Total</c>。</summary>
    public const string ProcessorTime = @"\Processor Information(*)\% Processor Time";

    /// <summary>
    /// 論理プロセッサごとの、定格クロックでの処理能力に対する今の処理量の割合（%）。タスク マネージャーが表示している値。
    /// ブースト中は 100 を超えることがある。
    /// </summary>
    public const string ProcessorUtility = @"\Processor Information(*)\% Processor Utility";

    /// <summary>論理プロセッサごとの、定格クロックに対する今の動作クロックの割合（%）。100 を超えるとブースト中。</summary>
    public const string ProcessorPerformance = @"\Processor Information(*)\% Processor Performance";

    /// <summary>論理プロセッサごとの定格クロック（MHz）。</summary>
    public const string ProcessorFrequency = @"\Processor Information(*)\Processor Frequency";

    /// <summary>ユーザー モード（アプリ）で使った時間の割合（%）。</summary>
    public const string ProcessorUserTimeTotal = @"\Processor Information(_Total)\% User Time";

    /// <summary>カーネル モード（Windows・ドライバー）で使った時間の割合（%）。</summary>
    public const string ProcessorPrivilegedTimeTotal = @"\Processor Information(_Total)\% Privileged Time";

    /// <summary>ハードウェア割り込みの処理に使った時間の割合（%）。</summary>
    public const string ProcessorInterruptTimeTotal = @"\Processor Information(_Total)\% Interrupt Time";

    /// <summary>遅延プロシージャ呼び出し（DPC）の処理に使った時間の割合（%）。</summary>
    public const string ProcessorDpcTimeTotal = @"\Processor Information(_Total)\% DPC Time";

    /// <summary>電力や温度による、最大クロックに対する制限の割合（%）。100 未満なら制限がかかっている。</summary>
    public const string ProcessorPerformanceLimitTotal = @"\Processor Information(_Total)\% Performance Limit";

    /// <summary>1 秒あたりのハードウェア割り込みの回数。</summary>
    public const string ProcessorInterruptsPerSecondTotal = @"\Processor Information(_Total)\Interrupts/sec";

    /// <summary>実行を待っているスレッドの数。</summary>
    public const string ProcessorQueueLength = @"\System\Processor Queue Length";

    /// <summary>1 秒あたりのスレッドの切り替え回数。</summary>
    public const string ContextSwitchesPerSecond = @"\System\Context Switches/sec";

    /// <summary>1 秒あたりのシステム コールの回数。</summary>
    public const string SystemCallsPerSecond = @"\System\System Calls/sec";

    /// <summary>1 秒あたりのページ フォールトの回数（ソフト・ハードの合計）。</summary>
    public const string PageFaultsPerSecond = @"\Memory\Page Faults/sec";

    /// <summary>1 秒あたりにディスクとやり取りしたページの数（ハード フォールト）。</summary>
    public const string PagesPerSecond = @"\Memory\Pages/sec";

    /// <summary>変更済みでディスクへの書き出しを待っているメモリ（バイト）。</summary>
    public const string ModifiedPageListBytes = @"\Memory\Modified Page List Bytes";

    /// <summary>スタンバイ（優先度：通常）のキャッシュ（バイト）。</summary>
    public const string StandbyCacheNormalPriorityBytes = @"\Memory\Standby Cache Normal Priority Bytes";

    /// <summary>スタンバイ（優先度：予備）のキャッシュ（バイト）。</summary>
    public const string StandbyCacheReserveBytes = @"\Memory\Standby Cache Reserve Bytes";

    /// <summary>スタンバイ（優先度：最優先）のキャッシュ（バイト）。</summary>
    public const string StandbyCacheCoreBytes = @"\Memory\Standby Cache Core Bytes";

    /// <summary>何にも使われていないメモリ（バイト）。</summary>
    public const string FreeAndZeroPageListBytes = @"\Memory\Free & Zero Page List Bytes";

    /// <summary>ページ ファイルの使用率（%）。</summary>
    public const string PagingFileUsageTotal = @"\Paging File(_Total)\% Usage";

    /// <summary>GPU のエンジンごとの使用率（%）。インスタンスは <c>pid_..._luid_..._eng_..._engtype_3D</c> の形。</summary>
    public const string GpuEngineUtilization = @"\GPU Engine(*)\Utilization Percentage";

    /// <summary>GPU ごとの専用メモリの使用量（バイト）。インスタンスは <c>luid_..._phys_0</c> の形。</summary>
    public const string GpuDedicatedUsage = @"\GPU Adapter Memory(*)\Dedicated Usage";

    /// <summary>GPU ごとの共有メモリの使用量（バイト）。</summary>
    public const string GpuSharedUsage = @"\GPU Adapter Memory(*)\Shared Usage";

    /// <summary>GPU ごとのコミット済みメモリの合計（バイト）。</summary>
    public const string GpuTotalCommitted = @"\GPU Adapter Memory(*)\Total Committed";

    /// <summary>物理ディスクごとのアイドル時間の割合（%）。インスタンスは <c>0 C:</c> のような「番号 ドライブ」の形。</summary>
    public const string DiskIdleTime = @"\PhysicalDisk(*)\% Idle Time";

    /// <summary>物理ディスクごとの読み込み速度（バイト／秒）。</summary>
    public const string DiskReadBytesPerSecond = @"\PhysicalDisk(*)\Disk Read Bytes/sec";

    /// <summary>物理ディスクごとの書き込み速度（バイト／秒）。</summary>
    public const string DiskWriteBytesPerSecond = @"\PhysicalDisk(*)\Disk Write Bytes/sec";

    /// <summary>物理ディスクごとの 1 秒あたりの読み込み回数。</summary>
    public const string DiskReadsPerSecond = @"\PhysicalDisk(*)\Disk Reads/sec";

    /// <summary>物理ディスクごとの 1 秒あたりの書き込み回数。</summary>
    public const string DiskWritesPerSecond = @"\PhysicalDisk(*)\Disk Writes/sec";

    /// <summary>物理ディスクごとの 1 回の読み込みにかかった平均時間（秒）。</summary>
    public const string DiskSecondsPerRead = @"\PhysicalDisk(*)\Avg. Disk sec/Read";

    /// <summary>物理ディスクごとの 1 回の書き込みにかかった平均時間（秒）。</summary>
    public const string DiskSecondsPerWrite = @"\PhysicalDisk(*)\Avg. Disk sec/Write";

    /// <summary>物理ディスクごとの、処理を待っている要求の数。</summary>
    public const string DiskQueueLength = @"\PhysicalDisk(*)\Current Disk Queue Length";

    /// <summary>CPU の電力量計（RAPL など）による消費電力（mW）。インスタンスは <c>rapl_package0_pkg</c> の形。</summary>
    public const string EnergyMeterPower = @"\Energy Meter(*)\Power";

    /// <summary>ファームウェアの電力計による消費電力（mW）。対応する PC は少ない。</summary>
    public const string PowerMeterPower = @"\Power Meter(*)\Power";

    /// <summary>ACPI の温度センサー（サーマル ゾーン）の温度（0.1 K 単位）。</summary>
    public const string ThermalZoneHighPrecisionTemperature = @"\Thermal Zone Information(*)\High Precision Temperature";

    /// <summary>ACPI の温度センサーの温度（K）。高精度の値がない古い PC 用。</summary>
    public const string ThermalZoneTemperature = @"\Thermal Zone Information(*)\Temperature";

    /// <summary>熱による CPU の性能の制限（%）。100 なら制限なし。</summary>
    public const string ThermalZonePassiveLimit = @"\Thermal Zone Information(*)\% Passive Limit";

    /// <summary>熱による制限がかかっている理由のフラグ。0 なら制限なし。</summary>
    public const string ThermalZoneThrottleReasons = @"\Thermal Zone Information(*)\Throttle Reasons";

    /// <summary>ネットワーク アダプターごとの受信の速さ（バイト／秒）。インスタンスはアダプターの説明（<c>(</c> は <c>[</c> などに置き換わる）。</summary>
    public const string NetworkBytesReceivedPerSecond = @"\Network Interface(*)\Bytes Received/sec";

    /// <summary>ネットワーク アダプターごとの送信の速さ（バイト／秒）。</summary>
    public const string NetworkBytesSentPerSecond = @"\Network Interface(*)\Bytes Sent/sec";

    /// <summary>ネットワーク アダプターごとの今のリンク速度（ビット／秒）。</summary>
    public const string NetworkCurrentBandwidth = @"\Network Interface(*)\Current Bandwidth";

    /// <summary>プロセスごとの CPU 使用率（論理プロセッサ 1 つを 100 とする %）。インスタンスは <c>chrome:1234</c> のような「名前:PID」。</summary>
    public const string ProcessV2ProcessorTime = @"\Process V2(*)\% Processor Time";

    /// <summary>プロセスごとの読み込みの速さ（バイト／秒）。ファイル・デバイス・ネットワークへの入出力をすべて含む。</summary>
    public const string ProcessV2IoReadBytesPerSecond = @"\Process V2(*)\IO Read Bytes/sec";

    /// <summary>プロセスごとの書き込みの速さ（バイト／秒）。ファイル・デバイス・ネットワークへの入出力をすべて含む。</summary>
    public const string ProcessV2IoWriteBytesPerSecond = @"\Process V2(*)\IO Write Bytes/sec";

    /// <summary>プロセスごとの CPU 使用率（%）。<c>Process V2</c> がない古い Windows 用。インスタンスは <c>chrome#2</c> のような名前。</summary>
    public const string ProcessProcessorTime = @"\Process(*)\% Processor Time";

    /// <summary>プロセスごとの読み込みの速さ（バイト／秒）。<c>Process V2</c> がない古い Windows 用。</summary>
    public const string ProcessIoReadBytesPerSecond = @"\Process(*)\IO Read Bytes/sec";

    /// <summary>プロセスごとの書き込みの速さ（バイト／秒）。<c>Process V2</c> がない古い Windows 用。</summary>
    public const string ProcessIoWriteBytesPerSecond = @"\Process(*)\IO Write Bytes/sec";

    /// <summary>プロセスの PID。<c>Process V2</c> がない古い Windows で、インスタンス名と PID を結び付けるために使う。</summary>
    public const string ProcessId = @"\Process(*)\ID Process";

    /// <summary>
    /// すべてのインスタンスを合計した値を表すインスタンス名。
    /// </summary>
    public const string TotalInstance = "_Total";
}
