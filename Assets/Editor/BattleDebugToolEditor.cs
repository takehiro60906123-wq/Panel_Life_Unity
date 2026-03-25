// =============================================================
// BattleDebugToolEditor.cs
// Inspector にデバッグボタンを表示するカスタムエディタ
//
// 配置:
//   Assets/Editor/ フォルダに配置すること。
//   （Editor フォルダ外に置くとビルドエラーになる）
// =============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BattleDebugTool))]
public class BattleDebugToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BattleDebugTool tool = (BattleDebugTool)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "再生中にボタンが有効になります。\n" +
                "キーボードショートカット: F1〜F8",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("デバッグ操作", EditorStyles.boldLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("─ 敵操作 ─", EditorStyles.miniLabel);

        Color defaultBg = GUI.backgroundColor;

        // 敵即死
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("F1  敵を即死させる", GUILayout.Height(28)))
        {
            tool.KillCurrentEnemy();
        }

        // ボス出現
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("F2  ボスを強制出現", GUILayout.Height(32)))
        {
            tool.ForceSpawnBoss();
        }

        // デバッグ敵出現
        GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
        if (GUILayout.Button("F8  デバッグ敵を強制出現", GUILayout.Height(28)))
        {
            tool.ForceSpawnDebugEnemy();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("─ プレイヤー操作 ─", EditorStyles.miniLabel);

        // 銃ゲージ最大
        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("F3  銃ゲージ最大", GUILayout.Height(28)))
        {
            tool.MaxGunGauge();
        }

        // 全回復
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("F4  プレイヤー全回復", GUILayout.Height(28)))
        {
            tool.FullHealPlayer();
        }

        // HP = 1
        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        if (GUILayout.Button("F5  プレイヤーHP → 1", GUILayout.Height(28)))
        {
            tool.SetPlayerHpToOne();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("─ その他 ─", EditorStyles.miniLabel);

        // コイン追加
        GUI.backgroundColor = new Color(1f, 0.95f, 0.4f);
        if (GUILayout.Button("F6  コイン +100", GUILayout.Height(24)))
        {
            tool.AddDebugCoins();
        }

        // 盤面再生成
        GUI.backgroundColor = new Color(0.7f, 0.7f, 1f);
        if (GUILayout.Button("F7  盤面を再生成", GUILayout.Height(24)))
        {
            tool.RegenerateBoard();
        }

        GUI.backgroundColor = defaultBg;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "ショートカット一覧:\n" +
            "F1: 敵即死  F2: ボス出現  F3: ゲージ最大\n" +
            "F4: 全回復  F5: HP→1  F6: コイン+100\n" +
            "F7: 盤面再生成  F8: デバッグ敵出現",
            MessageType.None);
    }
}
#endif
