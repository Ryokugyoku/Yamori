namespace Yamori.Core.Hardware;

/// <summary>
/// GPU の一覧から CPU の内蔵 GPU を取り出し、CPU の情報に移す。内蔵 GPU は CPU の一部として分類するため。
/// </summary>
public static class IntegratedGpuAssigner
{
    /// <summary>
    /// 内蔵 GPU を CPU の情報に移す。
    /// </summary>
    /// <param name="cpu">CPU の情報。null の場合は GPU の一覧をそのまま返す。</param>
    /// <param name="gpus">読み取った GPU の一覧（内蔵 GPU を含む）。</param>
    /// <returns>内蔵 GPU を設定した CPU の情報と、内蔵 GPU を除いた GPU の一覧。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gpus"/> が null。</exception>
    public static (CpuInfo? Cpu, IReadOnlyList<GpuInfo> Gpus) Assign(CpuInfo? cpu, IReadOnlyList<GpuInfo> gpus)
    {
        ArgumentNullException.ThrowIfNull(gpus);
        if (cpu is null)
        {
            return (cpu, gpus);
        }

        // CPU の内蔵 GPU は 1 つだけ。2 つ目以降が内蔵と報告されても、GPU の一覧に残す。
        var integrated = gpus.FirstOrDefault(g => g.IsIntegrated == true);
        if (integrated is null)
        {
            return (cpu, gpus);
        }

        return (cpu with { IntegratedGpu = integrated }, gpus.Where(g => !ReferenceEquals(g, integrated)).ToArray());
    }
}
