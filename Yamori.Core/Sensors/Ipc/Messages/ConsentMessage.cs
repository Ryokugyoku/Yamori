namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// PawnIO の利用の同意（取得の返事と、変更の依頼）。
/// </summary>
/// <param name="Enabled">同意しているなら true。</param>
public sealed record ConsentMessage(bool Enabled);
