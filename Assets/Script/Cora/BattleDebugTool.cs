// =============================================================
// BattleDebugTool.cs
// 戦闘テスト用デバッグツール
//
// 配置:
//   PanelBattleManager と同じ GameObject にアタッチ。
//   Inspector で参照とテスト用ボスプレハブを設定する。
//
// キーボードショートカット:
//   F1  現在の敵を即死させる（通常撃破フロー）
//   F2  ボスを強制出現させる
//   F3  銃ゲージを最大にする
//   F4  プレイヤーを全回復する
//   F5  プレイヤーHPを1にする（危機テスト）
//   F6  コインを +100 追加
//   F7  盤面を強制再生成する
//
// 注意:
//   ビルド時は UNITY_EDITOR or DEBUG_BUILD のみ動作。
//   リリースビルドには含まれない。
// =============================================================
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class BattleDebugTool : MonoBehaviour
{
    [Header("参照（自動取得可）")]
    [SerializeField] private PanelBattleManager battleManager;
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("テスト用ボスプレハブ")]
    [Tooltip("F2 で強制出現させるボスプレハブ。EnemyType が Boss のプレハブを設定する。")]
    [SerializeField] private BattleUnit bossPrefabToTest;

    [Header("テスト用通常敵プレハブ（任意）")]
    [Tooltip("F8 で強制出現させる敵プレハブ。任意の敵をすぐテストしたい時に使う。")]
    [SerializeField] private BattleUnit debugEnemyPrefab;

    [Header("設定")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private bool logActions = true;

    private bool isSpawningDebugEnemy;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = GetComponent<PanelBattleManager>();
        }

        if (playerCombatController == null)
        {
            playerCombatController = FindObjectOfType<PlayerCombatController>();
        }
    }

#if UNITY_EDITOR || DEBUG_BUILD

    private void Update()
    {
        if (!enableKeyboardShortcuts) return;

        if (Input.GetKeyDown(KeyCode.F1)) KillCurrentEnemy();
        if (Input.GetKeyDown(KeyCode.F2)) ForceSpawnBoss();
        if (Input.GetKeyDown(KeyCode.F3)) MaxGunGauge();
        if (Input.GetKeyDown(KeyCode.F4)) FullHealPlayer();
        if (Input.GetKeyDown(KeyCode.F5)) SetPlayerHpToOne();
        if (Input.GetKeyDown(KeyCode.F6)) AddDebugCoins();
        if (Input.GetKeyDown(KeyCode.F7)) RegenerateBoard();
        if (Input.GetKeyDown(KeyCode.F8)) ForceSpawnDebugEnemy();
        if (Input.GetKeyDown(KeyCode.F9)) TriggerStageClear();
        if (Input.GetKeyDown(KeyCode.F10)) TriggerGameOver();
    }

#endif

    // =============================================================
    // F1: 現在の敵を即死
    // =============================================================

    public void KillCurrentEnemy()
    {
        if (battleManager == null) return;

        BattleUnit enemy = battleManager.enemyUnit;
        if (enemy == null || enemy.IsDead())
        {
            Log("敵がいません");
            return;
        }

        int overkill = enemy.CurrentHP + enemy.CurrentShellHp + 999;
        enemy.TakeDamage(overkill);
        Log($"敵を即死させました (dmg={overkill})");
    }

    // =============================================================
    // F2: ボスを強制出現
    // =============================================================

    public void ForceSpawnBoss()
    {
        if (bossPrefabToTest == null)
        {
            Log("bossPrefabToTest が未設定です。Inspector で設定してください。");
            return;
        }

        ForceSpawnEnemy(bossPrefabToTest);
    }

    // =============================================================
    // F8: 任意の敵を強制出現
    // =============================================================

    public void ForceSpawnDebugEnemy()
    {
        if (debugEnemyPrefab == null)
        {
            Log("debugEnemyPrefab が未設定です。Inspector で設定してください。");
            return;
        }

        ForceSpawnEnemy(debugEnemyPrefab);
    }

    // =============================================================
    // 敵強制出現の共通処理
    // =============================================================

    private void ForceSpawnEnemy(BattleUnit prefab)
    {
        if (battleManager == null || prefab == null) return;

        if (isSpawningDebugEnemy)
        {
            Log("前のスポーン処理がまだ実行中です");
            return;
        }

        StartCoroutine(ForceSpawnEnemyRoutine(prefab));
    }

    private IEnumerator ForceSpawnEnemyRoutine(BattleUnit prefab)
    {
        isSpawningDebugEnemy = true;

        // 盤面をロック
        battleManager.SetBoardInteractable(false);

        // bossIntroController が未接続なら自動解決
        // （BattleBootstrapper の自動解決対象に含まれていないため）
        if (battleManager.bossIntroController == null)
        {
            BossIntroController found = battleManager.GetComponent<BossIntroController>();
            if (found == null)
            {
                found = FindObjectOfType<BossIntroController>();
            }
            if (found == null)
            {
                // 無ければ自動追加
                found = battleManager.gameObject.AddComponent<BossIntroController>();
                Log("BossIntroController を自動追加しました");
            }
            battleManager.bossIntroController = found;
            Log($"bossIntroController を自動接続しました: {found.name}");
        }

        // 現在の敵を静かに消す
        BattleUnit currentEnemy = battleManager.enemyUnit;
        if (currentEnemy != null)
        {
            // 演出を全部止める
            currentEnemy.transform.DOKill();
            SpriteRenderer[] renderers = currentEnemy.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null) sr.DOKill();
            }

            Destroy(currentEnemy.gameObject);
            battleManager.enemyUnit = null;

            yield return new WaitForSeconds(0.1f);
        }

        // 新しい敵をバトルポジションに生成
        Vector3 spawnPos = battleManager.battlePosition != null
            ? battleManager.battlePosition.position
            : Vector3.zero;

        BattleUnit newEnemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // ボステスト用: bossPrefabToTest から出した場合は enemyType を強制上書き
        // （ScriptableObject で管理している場合、プレハブ側が Normal のままのため）
        if (prefab == bossPrefabToTest)
        {
            newEnemy.enemyType = EnemyType.Boss;
            Log("enemyType を Boss に強制上書きしました");
        }

        battleManager.enemyUnit = newEnemy;

        // エンカウント状態を戦闘に設定
        battleManager.SetEncounterState(EncounterType.Enemy, 0);

        Log($"[{newEnemy.name}] を強制出現させます (Type={newEnemy.enemyType})");

        // 最終チェックログ
        Log($"bossIntroController={battleManager.bossIntroController != null}, " +
            $"IsBoss={BossIntroController.IsBossUnit(newEnemy)}, " +
            $"enemyUnit={battleManager.enemyUnit != null}");

        // 既存の登場演出ルーチンを通す
        // → ボスなら BossIntroController が自動で走る
        Log("PlayEnemyEntranceRoutine 開始");
        yield return StartCoroutine(battleManager.PlayEnemyEntranceRoutine(newEnemy));
        Log("PlayEnemyEntranceRoutine 完了");

        yield return new WaitForSeconds(0.2f);

        // 盤面をアンロック
        battleManager.SetBoardInteractable(true);

        isSpawningDebugEnemy = false;
    }

    // =============================================================
    // F3: 銃ゲージ最大
    // =============================================================

    public void MaxGunGauge()
    {
        if (playerCombatController == null)
        {
            Log("PlayerCombatController が見つかりません");
            return;
        }

        int max = playerCombatController.GetGunGaugeMax();
        int current = playerCombatController.GetGunGauge();
        int toAdd = max - current;

        if (toAdd > 0)
        {
            playerCombatController.AddGunGauge(toAdd);
        }

        Log($"銃ゲージ最大化: {max}/{max}");

        // UI 更新
        if (battleManager != null && battleManager.battleUIController != null)
        {
            battleManager.battleUIController.RefreshGunUI();
        }
    }

    // =============================================================
    // F4: プレイヤー全回復
    // =============================================================

    public void FullHealPlayer()
    {
        if (battleManager == null || battleManager.playerUnit == null)
        {
            Log("playerUnit が見つかりません");
            return;
        }

        BattleUnit player = battleManager.playerUnit;
        int missing = player.maxHP - player.CurrentHP;

        if (missing > 0)
        {
            player.Heal(missing);
        }

        Log($"プレイヤー全回復: {player.CurrentHP}/{player.maxHP}");
    }

    // =============================================================
    // F5: プレイヤーHPを1に（危機テスト）
    // =============================================================

    public void SetPlayerHpToOne()
    {
        if (battleManager == null || battleManager.playerUnit == null) return;

        BattleUnit player = battleManager.playerUnit;
        int toRemove = player.CurrentHP - 1;

        if (toRemove > 0)
        {
            player.TakeDamage(toRemove);
        }

        Log($"プレイヤーHP → 1/{player.maxHP}");
    }

    // =============================================================
    // F6: コイン追加
    // =============================================================

    public void AddDebugCoins()
    {
        if (battleManager == null) return;

        battleManager.AddCoins(100);
        Log("コイン +100");
    }

    // =============================================================
    // F7: 盤面強制再生成
    // =============================================================

    public void RegenerateBoard()
    {
        if (battleManager == null || battleManager.panelBoardController == null) return;

        battleManager.panelBoardController.GenerateBoard();
        Log("盤面を再生成しました");
    }

    // =============================================================
    // F9: ステージクリア演出テスト
    // =============================================================

    public void TriggerStageClear()
    {
        if (battleManager == null) return;

        Log("ステージクリアを強制発動");
        battleManager.OnStageClear();
    }

    // =============================================================
    // F10: ゲームオーバー演出テスト
    // =============================================================

    public void TriggerGameOver()
    {
        if (battleManager == null) return;

        Log("ゲームオーバーを強制発動");
        battleManager.OnPlayerDefeated();
    }

    // =============================================================
    // ユーティリティ
    // =============================================================

    private void Log(string message)
    {
        if (logActions)
        {
            Debug.Log($"<color=#FFD700>[DEBUG]</color> {message}");
        }
    }

    // =============================================================
    // Inspector ボタン用（Editor スクリプトなしで使えるように）
    // =============================================================

#if UNITY_EDITOR

    [ContextMenu("Debug: Kill Current Enemy (F1)")]
    private void EditorKillEnemy() => KillCurrentEnemy();

    [ContextMenu("Debug: Force Spawn Boss (F2)")]
    private void EditorSpawnBoss() => ForceSpawnBoss();

    [ContextMenu("Debug: Max Gun Gauge (F3)")]
    private void EditorMaxGauge() => MaxGunGauge();

    [ContextMenu("Debug: Full Heal Player (F4)")]
    private void EditorFullHeal() => FullHealPlayer();

    [ContextMenu("Debug: Player HP = 1 (F5)")]
    private void EditorLowHp() => SetPlayerHpToOne();

    [ContextMenu("Debug: Add 100 Coins (F6)")]
    private void EditorAddCoins() => AddDebugCoins();

    [ContextMenu("Debug: Regenerate Board (F7)")]
    private void EditorRegenBoard() => RegenerateBoard();

    [ContextMenu("Debug: Force Spawn Debug Enemy (F8)")]
    private void EditorSpawnDebugEnemy() => ForceSpawnDebugEnemy();

    [ContextMenu("Debug: Trigger Stage Clear (F9)")]
    private void EditorStageClear() => TriggerStageClear();

    [ContextMenu("Debug: Trigger Game Over (F10)")]
    private void EditorGameOver() => TriggerGameOver();

#endif
}
