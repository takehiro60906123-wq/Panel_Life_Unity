// =============================================================
// TintHelper.cs
// 影など色を変えたくない SpriteRenderer を自動除外するヘルパー
//
// 除外条件（いずれかに該当すれば除外）:
//   1. IgnoreTintMarker コンポーネントがついている
//   2. GameObject の名前に "Shadow" または "shadow" が含まれる
//
// プレハブの変更不要。名前規則だけで動く。
// =============================================================
using System.Collections.Generic;
using UnityEngine;

public static class TintHelper
{
    private static readonly List<SpriteRenderer> tempList = new List<SpriteRenderer>(16);

    /// <summary>
    /// 色変更（フラッシュ・シルエット等）の対象。Shadow を除外する。
    /// </summary>
    public static SpriteRenderer[] GetTintableRenderers(Component root)
    {
        if (root == null) return System.Array.Empty<SpriteRenderer>();

        tempList.Clear();
        SpriteRenderer[] all = root.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i];
            if (sr == null) continue;
            if (ShouldIgnoreTint(sr.gameObject)) continue;
            tempList.Add(sr);
        }

        return tempList.ToArray();
    }

    /// <summary>
    /// 表示制御（フェードアウト・非表示・死亡等）の対象。Shadow も含む全て。
    /// </summary>
    public static SpriteRenderer[] GetAllRenderers(Component root)
    {
        if (root == null) return System.Array.Empty<SpriteRenderer>();
        return root.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private static bool ShouldIgnoreTint(GameObject go)
    {
        if (go.GetComponent<IgnoreTintMarker>() != null) return true;

        string name = go.name;
        if (name.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

        return false;
    }
}
