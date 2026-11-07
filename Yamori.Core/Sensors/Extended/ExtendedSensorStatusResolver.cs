namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// 拡張センサー（PawnIO）の状態を、入っているか・公式版か・同意したか・宣言に成功したかから決める。
/// </summary>
public static class ExtendedSensorStatusResolver
{
    /// <summary>
    /// GUI のプロセスの中の状態を決める。GUI のプロセスでは PawnIO を使わない。
    /// </summary>
    /// <param name="installed">PawnIO が入っているかどうか。</param>
    /// <returns>入っていなければ <see cref="ExtendedSensorState.NotInstalled"/>、入っていれば <see cref="ExtendedSensorState.HelperRequired"/>。</returns>
    public static ExtendedSensorState ResolveOutsideHelper(bool installed) =>
        installed ? ExtendedSensorState.HelperRequired : ExtendedSensorState.NotInstalled;

    /// <summary>
    /// Helper のプロセスの中の状態を決める。
    /// </summary>
    /// <param name="installed">PawnIO が入っているかどうか。</param>
    /// <param name="official">PawnIO が公式版（署名済み）かどうか。</param>
    /// <param name="consented">ユーザーが PawnIO の利用に同意しているかどうか。</param>
    /// <param name="discoveryFailed">PawnIO の Provider の宣言（読み取り）に失敗したかどうか。</param>
    /// <returns>状態。PawnIO の Provider を使ってよいのは、<paramref name="installed"/>・<paramref name="official"/>・<paramref name="consented"/> がすべて true のときだけ。</returns>
    public static ExtendedSensorState ResolveInHelper(bool installed, bool official, bool consented, bool discoveryFailed) =>
        !installed ? ExtendedSensorState.NotInstalled
        : !official ? ExtendedSensorState.UnsupportedEdition
        : !consented ? ExtendedSensorState.ConsentRequired
        : discoveryFailed ? ExtendedSensorState.Unavailable
        : ExtendedSensorState.Active;
}
