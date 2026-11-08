using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Yamori.Views;

/// <summary>
/// 閉じた画面の一覧を、共有の状態の一覧から切り離す。
/// </summary>
/// <remarks>
/// 概要・ハードウェア・アプリの画面は、開くたびに作り直すが、一覧は 1 つのビューモデルのものを共有している。
/// 切り離さないと、閉じた画面の一覧（ItemsRepeater など）が残り続け、更新のたびに画面の外で書き換えられる。
/// </remarks>
public static class DetachedViewCleaner
{
    /// <summary>
    /// <paramref name="root"/> の中の一覧の表示元を外す。画面が閉じられたとき（Unloaded）に呼ぶ。
    /// </summary>
    /// <param name="root">閉じた画面。</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> が null。</exception>
    public static void Release(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        switch (root)
        {
            case ItemsRepeater repeater:
                repeater.ItemsSource = null;
                break;
            case ItemsControl items:
                items.ItemsSource = null;
                break;
        }

        // 一覧の中の要素は外した時点で片付くため、それ以外の子だけをたどる。
        if (root is ItemsRepeater or ItemsControl)
        {
            return;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            Release(VisualTreeHelper.GetChild(root, i));
        }
    }
}
