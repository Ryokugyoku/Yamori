using System.Runtime.InteropServices;

namespace Yamori.Core.Interop;

/// <summary>
/// グラフィックスカーネル（D3DKMT）の API の宣言。タスク マネージャーが GPU の温度やファンの回転数を表示するのと同じ仕組みで、
/// 管理者権限もドライバーの追加も必要としない（値を公開するかどうかは GPU のドライバー次第）。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>ビデオメモリの大きさを取得する問い合わせ（KMTQAITYPE_GETSEGMENTSIZE）。</summary>
    internal const int KmtQueryGetSegmentSize = 3;

    /// <summary>アダプターの PCI バス上の位置を取得する問い合わせ（KMTQAITYPE_ADAPTERADDRESS）。</summary>
    internal const int KmtQueryAdapterAddress = 6;

    /// <summary>アダプターの名前などを取得する問い合わせ（KMTQAITYPE_ADAPTERREGISTRYINFO）。</summary>
    internal const int KmtQueryAdapterRegistryInfo = 8;

    /// <summary>アダプターの種類を取得する問い合わせ（KMTQAITYPE_ADAPTERTYPE）。</summary>
    internal const int KmtQueryAdapterType = 15;

    /// <summary>エンジン（ノード）ごとの動作クロックを取得する問い合わせ（KMTQAITYPE_NODEPERFDATA）。</summary>
    internal const int KmtQueryNodePerfData = 61;

    /// <summary>温度・ファン・電力などを取得する問い合わせ（KMTQAITYPE_ADAPTERPERFDATA）。</summary>
    internal const int KmtQueryAdapterPerfData = 62;

    /// <summary>温度の上限やファンの最大回転数を取得する問い合わせ（KMTQAITYPE_ADAPTERPERFDATA_CAPS）。</summary>
    internal const int KmtQueryAdapterPerfDataCaps = 63;

    /// <summary>D3DKMT_ADAPTERTYPE のうち、ソフトウェアで描画するアダプター（Microsoft Basic Render Driver など）を表すビット。</summary>
    internal const uint KmtAdapterTypeSoftwareDevice = 1u << 2;

    /// <summary>D3DKMT_ADAPTERREGISTRYINFO の 1 項目の文字数（MAX_PATH）。</summary>
    internal const int KmtRegistryStringLength = 260;

    /// <summary>
    /// GPU アダプターの一覧を取得し、それぞれを開く。開いたアダプターは <see cref="D3DKMTCloseAdapter"/> で閉じること。
    /// </summary>
    /// <param name="enumAdapters">一覧を受け取る構造体。<see cref="D3dkmtEnumAdapters2.Adapters"/> が 0 なら件数だけを返す。</param>
    /// <returns>NTSTATUS。成功は 0。</returns>
    [LibraryImport("gdi32.dll")]
    internal static partial int D3DKMTEnumAdapters2(ref D3dkmtEnumAdapters2 enumAdapters);

    /// <summary>
    /// 開いたアダプターについて情報を問い合わせる。
    /// </summary>
    /// <param name="queryAdapterInfo">問い合わせの内容と、結果を受け取るバッファー。</param>
    /// <returns>NTSTATUS。成功は 0。</returns>
    [LibraryImport("gdi32.dll")]
    internal static partial int D3DKMTQueryAdapterInfo(ref D3dkmtQueryAdapterInfo queryAdapterInfo);

    /// <summary>
    /// 開いたアダプターを閉じる。
    /// </summary>
    /// <param name="closeAdapter">閉じるアダプター。</param>
    /// <returns>NTSTATUS。成功は 0。</returns>
    [LibraryImport("gdi32.dll")]
    internal static partial int D3DKMTCloseAdapter(in D3dkmtCloseAdapter closeAdapter);

    /// <summary>D3DKMT_ADAPTERADDRESS 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtAdapterAddress
    {
        /// <summary>PCI のバス番号。</summary>
        public uint BusNumber;

        /// <summary>PCI のデバイス番号。</summary>
        public uint DeviceNumber;

        /// <summary>PCI のファンクション番号。</summary>
        public uint FunctionNumber;
    }

    /// <summary>D3DKMT_ENUMADAPTERS2 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtEnumAdapters2
    {
        /// <summary>アダプターの件数。入力ではバッファーの件数、出力では実際の件数。</summary>
        public uint NumAdapters;

        /// <summary><see cref="D3dkmtAdapterInfo"/> の配列へのポインター。</summary>
        public nint Adapters;
    }

    /// <summary>D3DKMT_ADAPTERINFO 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtAdapterInfo
    {
        /// <summary>開いたアダプターのハンドル。</summary>
        public uint Adapter;

        /// <summary>アダプターの LUID の下位 32 ビット。</summary>
        public uint LuidLowPart;

        /// <summary>アダプターの LUID の上位 32 ビット。</summary>
        public int LuidHighPart;

        /// <summary>映像出力の数。</summary>
        public uint NumOfSources;

        /// <summary>BOOL。使わない。</summary>
        public int PrecisePresentRegionsPreferred;
    }

    /// <summary>D3DKMT_QUERYADAPTERINFO 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtQueryAdapterInfo
    {
        /// <summary>問い合わせるアダプターのハンドル。</summary>
        public uint Adapter;

        /// <summary>問い合わせの種類（KMTQUERYADAPTERINFOTYPE）。</summary>
        public int Type;

        /// <summary>結果を受け取るバッファーへのポインター。</summary>
        public nint PrivateDriverData;

        /// <summary>バッファーの大きさ（バイト）。</summary>
        public uint PrivateDriverDataSize;
    }

    /// <summary>D3DKMT_CLOSEADAPTER 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtCloseAdapter
    {
        /// <summary>閉じるアダプターのハンドル。</summary>
        public uint Adapter;
    }

    /// <summary>D3DKMT_SEGMENTSIZEINFO 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtSegmentSizeInfo
    {
        /// <summary>専用ビデオメモリの大きさ（バイト）。</summary>
        public ulong DedicatedVideoMemorySize;

        /// <summary>専用に確保されたシステムメモリの大きさ（バイト）。</summary>
        public ulong DedicatedSystemMemorySize;

        /// <summary>共有できるシステムメモリの大きさ（バイト）。</summary>
        public ulong SharedSystemMemorySize;
    }

    /// <summary>D3DKMT_ADAPTER_PERFDATA 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtAdapterPerfData
    {
        /// <summary>入力：物理アダプターの番号（通常は 0）。</summary>
        public uint PhysicalAdapterIndex;

        /// <summary>ビデオメモリの今の動作周波数（Hz）。</summary>
        public ulong MemoryFrequency;

        /// <summary>ビデオメモリの最大動作周波数（Hz）。</summary>
        public ulong MaxMemoryFrequency;

        /// <summary>ビデオメモリのオーバークロック時の最大周波数（Hz）。</summary>
        public ulong MaxMemoryFrequencyOC;

        /// <summary>ビデオメモリの帯域の使用量（バイト／秒）。</summary>
        public ulong MemoryBandwidth;

        /// <summary>PCI Express の帯域の使用量（バイト／秒）。</summary>
        public ulong PcieBandwidth;

        /// <summary>ファンの回転数（RPM）。</summary>
        public uint FanRpm;

        /// <summary>消費電力（TDP に対する割合を 10 倍した値）。</summary>
        public uint Power;

        /// <summary>温度（0.1 ℃ 単位）。</summary>
        public uint Temperature;

        /// <summary>電源状態の上書きの有無。使わない。</summary>
        public byte PowerStateOverride;
    }

    /// <summary>D3DKMT_ADAPTER_PERFDATACAPS 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtAdapterPerfDataCaps
    {
        /// <summary>入力：物理アダプターの番号（通常は 0）。</summary>
        public uint PhysicalAdapterIndex;

        /// <summary>ビデオメモリの最大帯域（バイト／秒）。</summary>
        public ulong MaxMemoryBandwidth;

        /// <summary>PCI Express の最大帯域（バイト／秒）。</summary>
        public ulong MaxPcieBandwidth;

        /// <summary>ファンの最大回転数（RPM）。</summary>
        public uint MaxFanRpm;

        /// <summary>温度の上限（0.1 ℃ 単位）。</summary>
        public uint TemperatureMax;

        /// <summary>警告を出す温度（0.1 ℃ 単位）。</summary>
        public uint TemperatureWarning;
    }

    /// <summary>D3DKMT_NODE_PERFDATA 構造体。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3dkmtNodePerfData
    {
        /// <summary>入力：エンジン（ノード）の番号。</summary>
        public uint NodeOrdinal;

        /// <summary>入力：物理アダプターの番号（通常は 0）。</summary>
        public uint PhysicalAdapterIndex;

        /// <summary>今の動作周波数（Hz）。</summary>
        public ulong Frequency;

        /// <summary>最大動作周波数（Hz）。</summary>
        public ulong MaxFrequency;

        /// <summary>オーバークロック時の最大周波数（Hz）。</summary>
        public ulong MaxFrequencyOC;

        /// <summary>今の電圧（mV）。</summary>
        public uint Voltage;

        /// <summary>最大電圧（mV）。</summary>
        public uint VoltageMax;

        /// <summary>オーバークロック時の最大電圧（mV）。</summary>
        public uint VoltageMaxOC;

        /// <summary>動作状態の切り替えにかかる最大時間（100 ナノ秒単位）。</summary>
        public ulong MaxTransitionLatency;
    }
}
