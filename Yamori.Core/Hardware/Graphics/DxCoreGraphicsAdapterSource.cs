using System.Runtime.InteropServices;
using Yamori.Core.Interop;

namespace Yamori.Core.Hardware.Graphics;

/// <summary>
/// DXCore（Windows 10 2004 以降の DirectX の API）から GPU の詳しい情報を読む。管理者権限もドライバーも不要。
/// </summary>
/// <remarks>
/// DXCore の COM インターフェースを、仮想関数テーブルの関数ポインターで直接呼ぶ。
/// 使うのは読み取りのメソッドだけで、GPU の設定は一切変えない。
/// DXCore がない古い Windows では空の一覧を返す。
/// </remarks>
public sealed unsafe class DxCoreGraphicsAdapterSource : IGraphicsAdapterSource
{
    // dxcore_interface.h のインターフェース ID。
    private static readonly Guid IidAdapterFactory = new("78ee5945-c36e-4b13-a669-005dd11c0f06");
    private static readonly Guid IidAdapterList = new("526c7776-40e9-459b-b711-f32ad76dfc28");
    private static readonly Guid IidAdapter = new("f0db4c7f-fe5a-42a2-bd62-f2a6cf6fc83e");

    // DXCORE_ADAPTER_ATTRIBUTE_D3D11_GRAPHICS：画面を描けるアダプター（一般的な GPU はすべて当てはまる）。
    private static readonly Guid AttributeD3D11Graphics = new("8c47866b-7583-450d-f0f0-6bada895af4b");

    // 仮想関数テーブル上の位置（0〜2 は IUnknown）。
    private const int ReleaseSlot = 2;
    private const int FactoryCreateAdapterListSlot = 3;
    private const int ListGetAdapterSlot = 3;
    private const int ListGetAdapterCountSlot = 4;
    private const int AdapterIsPropertySupportedSlot = 5;
    private const int AdapterGetPropertySlot = 6;

    /// <inheritdoc />
    public IReadOnlyList<GraphicsAdapterDetails> GetAdapters()
    {
        try
        {
            return ReadAdapters();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or COMException)
        {
            // DXCore がない Windows では、WMI の情報だけで表示を続ける。
            return [];
        }
    }

    private static List<GraphicsAdapterDetails> ReadAdapters()
    {
        var adapters = new List<GraphicsAdapterDetails>();
        if (NativeMethods.DXCoreCreateAdapterFactory(IidAdapterFactory, out var factory) < 0 || factory == 0)
        {
            return adapters;
        }

        try
        {
            var list = CreateAdapterList(factory);
            if (list == 0)
            {
                return adapters;
            }

            try
            {
                var count = GetAdapterCount(list);
                for (uint i = 0; i < count; i++)
                {
                    var adapter = GetAdapter(list, i);
                    if (adapter == 0)
                    {
                        continue;
                    }

                    try
                    {
                        if (ReadAdapter(adapter) is { } details)
                        {
                            adapters.Add(details);
                        }
                    }
                    finally
                    {
                        Release(adapter);
                    }
                }
            }
            finally
            {
                Release(list);
            }
        }
        finally
        {
            Release(factory);
        }

        return adapters;
    }

    private static GraphicsAdapterDetails? ReadAdapter(nint adapter)
    {
        // ソフトウェアで描画するアダプター（Microsoft Basic Render Driver）は除く。
        if (GetBool(adapter, AdapterProperty.IsHardware) != true)
        {
            return null;
        }

        if (!TryGetProperty(adapter, AdapterProperty.HardwareId, out HardwareId id))
        {
            return null;
        }

        return new GraphicsAdapterDetails(
            new PciDeviceId(id.VendorId, id.DeviceId),
            GetUInt64(adapter, AdapterProperty.DedicatedAdapterMemory),
            GetUInt64(adapter, AdapterProperty.SharedSystemMemory),
            GetBool(adapter, AdapterProperty.IsIntegrated));
    }

    private static nint CreateAdapterList(nint factory)
    {
        var attribute = AttributeD3D11Graphics;
        var iid = IidAdapterList;
        nint list = 0;
        var createAdapterList = (delegate* unmanaged[Stdcall]<nint, uint, Guid*, Guid*, nint*, int>)VTable(factory)[FactoryCreateAdapterListSlot];
        return createAdapterList(factory, 1, &attribute, &iid, &list) < 0 ? 0 : list;
    }

    private static uint GetAdapterCount(nint list)
    {
        var getAdapterCount = (delegate* unmanaged[Stdcall]<nint, uint>)VTable(list)[ListGetAdapterCountSlot];
        return getAdapterCount(list);
    }

    private static nint GetAdapter(nint list, uint index)
    {
        var iid = IidAdapter;
        nint adapter = 0;
        var getAdapter = (delegate* unmanaged[Stdcall]<nint, uint, Guid*, nint*, int>)VTable(list)[ListGetAdapterSlot];
        return getAdapter(list, index, &iid, &adapter) < 0 ? 0 : adapter;
    }

    private static ulong? GetUInt64(nint adapter, AdapterProperty property) =>
        TryGetProperty(adapter, property, out ulong value) ? value : null;

    private static bool? GetBool(nint adapter, AdapterProperty property) =>
        TryGetProperty(adapter, property, out byte value) ? value != 0 : null;

    private static bool TryGetProperty<T>(nint adapter, AdapterProperty property, out T value)
        where T : unmanaged
    {
        value = default;
        var vtable = VTable(adapter);
        var isPropertySupported = (delegate* unmanaged[Stdcall]<nint, uint, byte>)vtable[AdapterIsPropertySupportedSlot];
        if (isPropertySupported(adapter, (uint)property) == 0)
        {
            return false;
        }

        T result;
        var getProperty = (delegate* unmanaged[Stdcall]<nint, uint, nuint, void*, int>)vtable[AdapterGetPropertySlot];
        if (getProperty(adapter, (uint)property, (nuint)sizeof(T), &result) < 0)
        {
            return false;
        }

        value = result;
        return true;
    }

    private static void Release(nint comObject)
    {
        var release = (delegate* unmanaged[Stdcall]<nint, uint>)VTable(comObject)[ReleaseSlot];
        release(comObject);
    }

    private static nint* VTable(nint comObject) => *(nint**)comObject;

    /// <summary>
    /// DXCoreAdapterProperty の値（使うものだけ）。
    /// </summary>
    private enum AdapterProperty : uint
    {
        /// <summary>PCI のベンダー ID・デバイス ID など（DXCoreHardwareID）。</summary>
        HardwareId = 3,

        /// <summary>専用ビデオメモリ（uint64）。</summary>
        DedicatedAdapterMemory = 7,

        /// <summary>共有メモリ（uint64）。</summary>
        SharedSystemMemory = 9,

        /// <summary>ハードウェアのアダプターかどうか（bool）。</summary>
        IsHardware = 11,

        /// <summary>内蔵 GPU かどうか（bool）。</summary>
        IsIntegrated = 12,
    }

    /// <summary>
    /// DXCoreHardwareID 構造体。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareId
    {
        /// <summary>PCI のベンダー ID。</summary>
        public uint VendorId;

        /// <summary>PCI のデバイス ID。</summary>
        public uint DeviceId;

        /// <summary>サブシステム ID。</summary>
        public uint SubSystemId;

        /// <summary>リビジョン。</summary>
        public uint Revision;
    }
}
