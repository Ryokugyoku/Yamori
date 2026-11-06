namespace Yamori.Core.Recommendations;

/// <summary>
/// 部品の性能が、実際の使い方に対してどれくらい足りているかの判定。
/// </summary>
public enum RecommendationLevel
{
    /// <summary>判定に必要な量のデータがまだない。</summary>
    NotEnoughData,

    /// <summary>今の使い方には足りている。</summary>
    Sufficient,

    /// <summary>ときどき限界に近い。次の PC では 1 段上が安心。</summary>
    Borderline,

    /// <summary>限界に近い時間が長い。次の PC では 1 段上をおすすめする。</summary>
    Insufficient,
}
