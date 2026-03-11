using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 敵をタッチすると旧文明スキャナー風の情報パネルを表示する。
/// 
/// セットアップ：
///   1. Canvas 直下に空の Panel を作成（ScannerPanel）
///   2. ScannerPanel の中に以下の TMP テキストを配置：
///      - nameText, typeText, statsText, patternText, adviceText
///   3. ScannerPanel は初期状態で非表示にしておく
///   4. このスクリプトを PanelBattleManager と同じ GameObject に付けるか、
///      Canvas 上の任意の GameObject に付ける
///   5. Inspector で各参照をセット
/// </summary>
public class EnemyScannerUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private PanelBattleManager panelBattleManager;

    [Header("スキャナーパネル")]
    [SerializeField] private RectTransform scannerPanel;
    [SerializeField] private CanvasGroup scannerCanvasGroup;

    [Header("表示テキスト")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text patternText;
    [SerializeField] private TMP_Text adviceText;

    [Header("スキャンライン（任意）")]
    [SerializeField] private RectTransform scanLineImage;

    [Header("演出設定")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.15f;
    [SerializeField] private float scanLineDuration = 0.4f;
    [SerializeField] private float tapRadius = 160f;

    [Header("色設定")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color floatingColor = new Color(0.6f, 0.9f, 1f);
    [SerializeField] private Color armoredColor = new Color(1f, 0.8f, 0.4f);
    [SerializeField] private Color rushingColor = new Color(1f, 0.5f, 0.5f);
    [SerializeField] private Color rangedColor = new Color(0.8f, 0.6f, 1f);

    private bool isOpen;
    private BattleUnit currentTarget;

    private void Awake()
    {
        if (scannerPanel != null)
        {
            scannerPanel.gameObject.SetActive(false);
        }

        isOpen = false;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // パネルが開いている → 閉じる
        if (isOpen)
        {
            CloseScanner();
            return;
        }

        // 敵をタップしたか判定
        BattleUnit tappedEnemy = DetectEnemyTap();
        if (tappedEnemy != null)
        {
            OpenScanner(tappedEnemy);
        }
    }

    // =============================================================
    // タップ検出
    // =============================================================

    private BattleUnit DetectEnemyTap()
    {
        BattleUnit enemyUnit = GetCurrentEnemy();
        if (enemyUnit == null || enemyUnit.IsDead()) return null;

        Vector2 screenPos = Input.mousePosition;
        Camera mainCam = Camera.main;
        if (mainCam == null) return null;

        // 敵のスクリーン座標を取得
        Vector3 enemyWorldPos = enemyUnit.transform.position + Vector3.up * 0.75f;
        Renderer enemyRenderer = enemyUnit.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            enemyWorldPos = enemyRenderer.bounds.center;
        }

        Vector2 enemyScreenPos = RectTransformUtility.WorldToScreenPoint(mainCam, enemyWorldPos);

        if (Vector2.Distance(screenPos, enemyScreenPos) <= tapRadius)
        {
            return enemyUnit;
        }

        return null;
    }

    private BattleUnit GetCurrentEnemy()
    {
        if (panelBattleManager != null)
        {
            return panelBattleManager.enemyUnit;
        }

        return null;
    }

    // =============================================================
    // スキャナー開閉
    // =============================================================

    private void OpenScanner(BattleUnit enemy)
    {
        if (scannerPanel == null) return;
        if (enemy == null) return;

        currentTarget = enemy;
        isOpen = true;

        // テキスト設定
        SetScannerTexts(enemy);

        // 表示
        scannerPanel.gameObject.SetActive(true);

        // 演出：スケール＋フェードイン
        scannerPanel.localScale = new Vector3(1f, 0f, 1f);

        if (scannerCanvasGroup != null)
        {
            scannerCanvasGroup.alpha = 0f;
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(scannerPanel.DOScaleY(1f, openDuration).SetEase(Ease.OutBack));

        if (scannerCanvasGroup != null)
        {
            seq.Join(scannerCanvasGroup.DOFade(1f, openDuration));
        }

        // スキャンライン演出
        if (scanLineImage != null)
        {
            scanLineImage.gameObject.SetActive(true);

            float panelHeight = scannerPanel.rect.height;
            scanLineImage.anchoredPosition = new Vector2(0, panelHeight * 0.5f);

            seq.Append(scanLineImage.DOAnchorPosY(-panelHeight * 0.5f, scanLineDuration).SetEase(Ease.Linear));
            seq.AppendCallback(() =>
            {
                if (scanLineImage != null)
                    scanLineImage.gameObject.SetActive(false);
            });
        }
    }

    private void CloseScanner()
    {
        if (scannerPanel == null) return;

        isOpen = false;
        currentTarget = null;

        Sequence seq = DOTween.Sequence();

        if (scannerCanvasGroup != null)
        {
            seq.Append(scannerCanvasGroup.DOFade(0f, closeDuration));
        }

        seq.Join(scannerPanel.DOScaleY(0f, closeDuration).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            if (scannerPanel != null)
                scannerPanel.gameObject.SetActive(false);
        });
    }

    // =============================================================
    // テキスト生成
    // =============================================================

    private void SetScannerTexts(BattleUnit enemy)
    {
        if (enemy == null) return;

        // --- 名前 ---
        if (nameText != null)
        {
            string lvStr = enemy.enemyLevel > 0 ? $"  Lv+{enemy.enemyLevel}" : "";
            nameText.text = $"[ SCAN ] {enemy.name.Replace("(Clone)", "")}{lvStr}";
        }

        // --- タイプ ---
        Color typeColor = GetTypeColor(enemy.enemyType);

        if (typeText != null)
        {
            typeText.text = GetTypeLabel(enemy.enemyType);
            typeText.color = typeColor;
        }

        // --- ステータス ---
        if (statsText != null)
        {
            statsText.text = $"HP {enemy.CurrentHP}/{enemy.maxHP}    ATK {enemy.attackPower}    EXP {enemy.expYield}";
        }

        // --- 攻撃パターン ---
        if (patternText != null)
        {
            patternText.text = GetPatternLabel(enemy.attackPattern, enemy.attackPower);
        }

        // --- 攻略アドバイス ---
        if (adviceText != null)
        {
            adviceText.text = GetAdviceText(enemy.enemyType, enemy.attackPattern);
            adviceText.color = typeColor;
        }
    }

    // =============================================================
    // タイプ関連テキスト
    // =============================================================

    private string GetTypeLabel(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating: return "TYPE: FLOATING";
            case EnemyType.Armored:  return "TYPE: ARMORED";
            case EnemyType.Rushing:  return "TYPE: RUSHING";
            case EnemyType.Ranged:   return "TYPE: RANGED";
            default:                 return "TYPE: NORMAL";
        }
    }

    private Color GetTypeColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating: return floatingColor;
            case EnemyType.Armored:  return armoredColor;
            case EnemyType.Rushing:  return rushingColor;
            case EnemyType.Ranged:   return rangedColor;
            default:                 return normalColor;
        }
    }

    // =============================================================
    // 攻撃パターン説明
    // =============================================================

    private string GetPatternLabel(EnemyAttackPattern pattern, int atk)
    {
        switch (pattern)
        {
            case EnemyAttackPattern.HeavyHit:
                return $"攻撃: 重撃 — 1ターン溜め後 {atk * 2} ダメージ";

            case EnemyAttackPattern.MultiHit:
                return $"攻撃: 連撃 — {atk} × 2回攻撃";

            case EnemyAttackPattern.SelfBuff:
                return $"攻撃: {atk} — 時々自己回復 (+{atk * 2})";

            case EnemyAttackPattern.PanelCorrupt:
                return atk > 0
                    ? $"攻撃: {atk} + 盤面汚染"
                    : "攻撃なし — 盤面汚染のみ";

            default:
                return $"攻撃: {atk}";
        }
    }

    // =============================================================
    // 攻略アドバイス
    // =============================================================

    private string GetAdviceText(EnemyType type, EnemyAttackPattern pattern)
    {
        string typeAdvice = GetTypeAdvice(type);
        string patternAdvice = GetPatternAdvice(pattern);

        if (!string.IsNullOrEmpty(typeAdvice) && !string.IsNullOrEmpty(patternAdvice))
        {
            return $"{typeAdvice}\n{patternAdvice}";
        }

        if (!string.IsNullOrEmpty(typeAdvice)) return typeAdvice;
        if (!string.IsNullOrEmpty(patternAdvice)) return patternAdvice;

        return "特記事項なし";
    }

    private string GetTypeAdvice(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating:
                return "> 近接攻撃が通りにくい。銃器の使用を推奨";

            case EnemyType.Armored:
                return "> 装甲により小ダメージを軽減。一撃の大きい攻撃が有効";

            case EnemyType.Rushing:
                return "> 攻撃頻度が高い。迎撃系の銃が有効";

            case EnemyType.Ranged:
                return "> 遠距離から攻撃。早期排除を推奨";

            default:
                return "";
        }
    }

    private string GetPatternAdvice(EnemyAttackPattern pattern)
    {
        switch (pattern)
        {
            case EnemyAttackPattern.HeavyHit:
                return "> 溜め行動を確認したら銃で先に倒すことを推奨";

            case EnemyAttackPattern.MultiHit:
                return "> 被弾が多い。回復パネルの確保が重要";

            case EnemyAttackPattern.SelfBuff:
                return "> 放置すると回復される。火力で押すべし";

            case EnemyAttackPattern.PanelCorrupt:
                return "> 盤面にLvUpパネルを追加する。長引くと不利";

            default:
                return "";
        }
    }
}
