namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// テストの間だけ使う一時フォルダー。破棄すると中身ごと消す。
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "yamori-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // ネイティブのライブラリがまだファイルを閉じていない場合など。一時フォルダーなので残っても構わない。
        }
    }
}
