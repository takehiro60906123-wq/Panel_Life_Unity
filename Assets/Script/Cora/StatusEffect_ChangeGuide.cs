// =============================================================
// 状態異常システム（金縛り）追加 — 既存ファイル変更ガイド
// =============================================================
//
// このファイルは変更箇所の一覧です。
// 各セクションに「変更前」「変更後」を記載しています。
//
// 変更対象:
//   1. BattleUnit.cs
//   2. BattleDamageResolver.cs
//   3. BattleTurnController.cs
//   4. PanelBattleManager.cs
//
// =============================================================


// #############################################################
// 1. BattleUnit.cs
// #############################################################
//
// --- 変更 1-A: RequireComponent に StatusEffectHolder を追加 ---
//
// 【変更前】
//
//   [DisallowMultipleComponent]
//   [RequireComponent(typeof(PlayerProgression))]
//   [RequireComponent(typeof(EnemyTurnState))]
//   [RequireComponent(typeof(BattleUnitView))]
//   public class BattleUnit : MonoBehaviour
//
// 【変更後】
//
//   [DisallowMultipleComponent]
//   [RequireComponent(typeof(PlayerProgression))]
//   [RequireComponent(typeof(EnemyTurnState))]
//   [RequireComponent(typeof(BattleUnitView))]
//   [RequireComponent(typeof(StatusEffectHolder))]
//   public class BattleUnit : MonoBehaviour
//
//
// --- 変更 1-B: フィールドとプロパティを追加 ---
//
// 既存の private フィールド群:
//   private PlayerProgression progression;
//   private EnemyTurnState turnState;
//   private BattleUnitView view;
//
// の直後に追加:
//
//   private StatusEffectHolder statusEffects;
//   public StatusEffectHolder StatusEffects => statusEffects;
//
//
// --- 変更 1-C: Awake() 内で GetComponent を追加 ---
//
// 既存の Awake() 内:
//   view = GetComponent<BattleUnitView>();
//
// の直後に追加:
//
//   statusEffects = GetComponent<StatusEffectHolder>();
//


// #############################################################
// 2. BattleDamageResolver.cs
// #############################################################
//
// --- 変更 2-A: DamageEnemy() 内、ダメージ適用直後に被弾解除を追加 ---
//
// 【変更前】 DamageEnemy() 内、約123行目付近:
//
//   enemyUnit.TakeDamage(finalDamage, useHeavyReaction);
//   battleEventHub?.RaiseOneShotEffectRequested(hitEffectPrefab, enemyPos + Vector3.up * hitEffectHeight, hitEffectReturnDelay);
//
// 【変更後】
//
//   enemyUnit.TakeDamage(finalDamage, useHeavyReaction);
//
//   // --- 状態異常: 被弾解除 ---
//   if (enemyUnit.StatusEffects != null)
//   {
//       enemyUnit.StatusEffects.OnDamageReceived();
//   }
//
//   battleEventHub?.RaiseOneShotEffectRequested(hitEffectPrefab, enemyPos + Vector3.up * hitEffectHeight, hitEffectReturnDelay);
//


// #############################################################
// 3. BattleTurnController.cs
// #############################################################
//
// --- 変更 3-A: EnemyTurnRoutine() 冒頭に敵金縛りチェックを追加 ---
//
// 【変更前】 EnemyTurnRoutine() 内、enemyUnit null チェック直後:
//
//   if (enemyUnit == null)
//   {
//       setBoardInteractable?.Invoke(true);
//       yield break;
//   }
//
//   enemyUnit.TickCooldown();
//
// 【変更後】
//
//   if (enemyUnit == null)
//   {
//       setBoardInteractable?.Invoke(true);
//       yield break;
//   }
//
//   // === 状態異常: 敵金縛りチェック ===
//   // 金縛り中はクールダウンも進まない（完全凍結）
//   StatusEffectHolder enemyStatusHolder = enemyUnit.StatusEffects;
//   if (enemyStatusHolder != null && enemyStatusHolder.HasEffect(StatusEffectType.Paralysis))
//   {
//       Vector3 paralysisTextPos = enemyUnit.transform.position + Vector3.up * 1.5f;
//       spawnDamageText?.Invoke("金縛り！", paralysisTextPos, new Color(0.8f, 0.4f, 1f));
//
//       // 行動スキップした後に残りターンを消費
//       enemyStatusHolder.ConsumeEffectTurn(StatusEffectType.Paralysis);
//
//       yield return new WaitForSeconds(enemyIdleDelay);
//       setBoardInteractable?.Invoke(true);
//       yield break;
//   }
//
//   enemyUnit.TickCooldown();
//
//
// --- 変更 3-B: プレイヤー被弾時の状態異常解除（1発目） ---
//
// 【変更前】 EnemyTurnRoutine() 内、プレイヤーへのダメージ（1発目）:
//
//   if (playerUnit != null) playerUnit.TakeDamage(finalDamage);
//
//   if (hitEffectPrefab != null)
//
// 【変更後】
//
//   if (playerUnit != null) playerUnit.TakeDamage(finalDamage);
//
//   // --- 状態異常: プレイヤー被弾解除 ---
//   if (playerUnit != null && playerUnit.StatusEffects != null)
//   {
//       playerUnit.StatusEffects.OnDamageReceived();
//   }
//
//   if (hitEffectPrefab != null)
//
//
// --- 変更 3-C: プレイヤー被弾時の状態異常解除（MultiHit 2発目） ---
//
// 【変更前】 EnemyTurnRoutine() 内、MultiHit 2発目:
//
//   int finalDmg2 = hit2Crit ? hit2Damage * 2 : hit2Damage;
//   playerUnit.TakeDamage(finalDmg2);
//
//   if (hitEffectPrefab != null)
//
// 【変更後】
//
//   int finalDmg2 = hit2Crit ? hit2Damage * 2 : hit2Damage;
//   playerUnit.TakeDamage(finalDmg2);
//
//   // --- 状態異常: プレイヤー被弾解除 ---
//   if (playerUnit.StatusEffects != null)
//   {
//       playerUnit.StatusEffects.OnDamageReceived();
//   }
//
//   if (hitEffectPrefab != null)
//


// #############################################################
// 4. PanelBattleManager.cs
// #############################################################
//
// --- 変更 4-A: 状態異常スキップ用の設定フィールドを追加 ---
//
// 既存の [Header("アイテム設定")] の直前あたりに追加:
//
//   [Header("状態異常")]
//   [SerializeField] private float paralysisSkipDelay = 0.6f;
//
//
// --- 変更 4-B: SetBoardInteractable() にプレイヤー金縛りチェックを追加 ---
//
// 【変更前】
//
//   public void SetBoardInteractable(bool isInteractable)
//   {
//       isPlayerTurn = isInteractable;
//
//       if (boardCanvasGroup != null)
//       {
//           boardCanvasGroup.interactable = isInteractable;
//           boardCanvasGroup.blocksRaycasts = isInteractable;
//           boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
//       }
//   }
//
// 【変更後】
//
//   public void SetBoardInteractable(bool isInteractable)
//   {
//       // --- 状態異常: プレイヤー金縛りチェック ---
//       // 戦闘中にプレイヤーターンが始まろうとしたとき、
//       // 金縛り中なら盤面を開放せずターンをスキップする。
//       if (isInteractable
//           && currentEncounter == EncounterType.Enemy
//           && playerUnit != null
//           && playerUnit.StatusEffects != null
//           && playerUnit.StatusEffects.HasEffect(StatusEffectType.Paralysis))
//       {
//           StartCoroutine(HandlePlayerParalysisSkip());
//           return;
//       }
//
//       isPlayerTurn = isInteractable;
//
//       if (boardCanvasGroup != null)
//       {
//           boardCanvasGroup.interactable = isInteractable;
//           boardCanvasGroup.blocksRaycasts = isInteractable;
//           boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
//       }
//   }
//
//
// --- 変更 4-C: HandlePlayerParalysisSkip コルーチンを追加 ---
//
// SetBoardInteractable() の直後あたりに新メソッドを追加:
//
//   /// <summary>
//   /// プレイヤー金縛り時のターンスキップ処理。
//   /// 盤面を開放せず、スキップ演出後に EndPlayerTurn() を呼ぶ。
//   /// EndPlayerTurn() → 敵ターン → SetBoardInteractable(true) と巡回し、
//   /// まだ金縛りが残っていれば再びここに来る。
//   /// </summary>
//   private System.Collections.IEnumerator HandlePlayerParalysisSkip()
//   {
//       // 盤面ロック状態を維持
//       isPlayerTurn = false;
//
//       if (boardCanvasGroup != null)
//       {
//           boardCanvasGroup.interactable = false;
//           boardCanvasGroup.blocksRaycasts = false;
//       }
//
//       // 演出テキスト
//       Vector3 textPos = playerUnit != null
//           ? playerUnit.transform.position + Vector3.up * 1.5f
//           : Vector3.up * 1.5f;
//       SpawnDamageText("金縛り！", textPos, new Color(0.8f, 0.4f, 1f));
//
//       yield return new WaitForSeconds(paralysisSkipDelay);
//
//       // 行動スキップ後に残りターンを消費
//       if (playerUnit != null && playerUnit.StatusEffects != null)
//       {
//           playerUnit.StatusEffects.ConsumeEffectTurn(StatusEffectType.Paralysis);
//       }
//
//       // 通常のターン終了処理へ（敵ターンに遷移する）
//       yield return StartCoroutine(EndPlayerTurn());
//   }
//


// #############################################################
// 補足: EncounterFlowController.cs のフォールバック敵ターン
// #############################################################
//
// EncounterFlowController には battleTurnController が null のときの
// フォールバック EnemyTurnRoutine() (248行目〜) がある。
// 本番では BattleTurnController 側のルートが使われるが、
// 念のためフォールバック側にも同様のチェックを入れたい場合は以下。
//
// --- 変更 5-A（任意）: EncounterFlowController.EnemyTurnRoutine() ---
//
// 【変更前】
//
//   enemyUnit.TickCooldown();
//
//   if (enemyUnit.IsReadyToAttack())
//
// 【変更後】
//
//   // === 状態異常: 敵金縛りチェック（フォールバック） ===
//   StatusEffectHolder fbEnemyStatus = enemyUnit.StatusEffects;
//   if (fbEnemyStatus != null && fbEnemyStatus.HasEffect(StatusEffectType.Paralysis))
//   {
//       RequestDamageText("金縛り！", enemyUnit.transform.position + Vector3.up * 1.5f, new Color(0.8f, 0.4f, 1f));
//       fbEnemyStatus.ConsumeEffectTurn(StatusEffectType.Paralysis);
//       yield return new WaitForSeconds(0.25f);
//       RequestBoardInteractable(true);
//       yield break;
//   }
//
//   enemyUnit.TickCooldown();
//
//   if (enemyUnit.IsReadyToAttack())
//
//
// --- 変更 5-B（任意）: プレイヤー被弾解除（フォールバック） ---
//
// 【変更前】
//
//   if (playerUnit != null)
//   {
//       playerUnit.TakeDamage(damage);
//   }
//
//   if (hitEffectPrefab != null)
//
// 【変更後】
//
//   if (playerUnit != null)
//   {
//       playerUnit.TakeDamage(damage);
//
//       // --- 状態異常: プレイヤー被弾解除（フォールバック） ---
//       if (playerUnit.StatusEffects != null)
//       {
//           playerUnit.StatusEffects.OnDamageReceived();
//       }
//   }
//
//   if (hitEffectPrefab != null)
//


// =============================================================
// 以上
// =============================================================
