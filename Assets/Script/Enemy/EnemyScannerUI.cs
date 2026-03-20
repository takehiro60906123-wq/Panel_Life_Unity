using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 敵をタッチするとスキャナーパネルで情報表示。
/// 敵の近くに「SCAN」ヒントが常時パルスして、タッチ可能であることを示す。
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
    [SerializeField] private TMP_Text weaknessText;

    [Header("スキャンライン（任意）")]
    [SerializeField] private RectTransform scanLineImage;

    [Header("スキャンヒント")]
    [Tooltip("敵の近くに表示される常時パルスするワールドスペースのオブジェクト。\n" +
             "SpriteRenderer または Canvas+Text を持つ小さいプレハブ。\n" +
             "なければスクリプトが自動で TextMesh を生成する。")]
    [SerializeField] private GameObject scanHintPrefab;
    [SerializeField] private Vector3 scanHintOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float scanHintPulseScale = 1.2f;
    [SerializeField] private float scanHintPulseDuration = 0.8f;

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
    private GameObject scanHintInstance;
    private BattleUnit lastTrackedEnemy;

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
        // --- スキャンヒントの追従 ---
       // UpdateScanHintPosition();

        if (!Input.GetMouseButtonDown(0)) return;

        if (isOpen)
        {
            CloseScanner();
            return;
        }

        BattleUnit tappedEnemy = DetectEnemyTap();
        if (tappedEnemy != null)
        {
            OpenScanner(tappedEnemy);
        }
    }

    // =============================================================
    // スキャンヒント（常時表示）
    // =============================================================

    private void UpdateScanHintPosition()
    {
        BattleUnit enemy = GetCurrentEnemy();

        // 敵が変わったらヒントを再生成
        if (enemy != lastTrackedEnemy)
        {
            lastTrackedEnemy = enemy;
            DestroyScanHint();

            if (enemy != null && !enemy.IsDead())
            {
                CreateScanHint(enemy);
            }
        }

        // ヒントを敵に追従
        if (scanHintInstance != null && enemy != null && !enemy.IsDead())
        {
            scanHintInstance.transform.position = enemy.transform.position + scanHintOffset;
        }

        // 敵が死んだらヒントを消す
        if (enemy != null && enemy.IsDead())
        {
            DestroyScanHint();
        }
    }

    private void CreateScanHint(BattleUnit enemy)
    {
        if (scanHintPrefab != null)
        {
            scanHintInstance = Instantiate(
                scanHintPrefab,
                enemy.transform.position + scanHintOffset,
                Quaternion.identity
            );
        }

        // プレハブが無い時の自動生成はしない
    }

    private void DestroyScanHint()
    {
        if (scanHintInstance != null)
        {
            scanHintInstance.transform.DOKill();
            Destroy(scanHintInstance);
            scanHintInstance = null;
        }
    }

    private void OnDestroy()
    {
        DestroyScanHint();
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
        if (scannerPanel == null || enemy == null) return;

        currentTarget = enemy;
        isOpen = true;

        SetScannerTexts(enemy);

        scannerPanel.gameObject.SetActive(true);
        scannerPanel.localScale = new Vector3(1f, 0f, 1f);

        if (scannerCanvasGroup != null) scannerCanvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Append(scannerPanel.DOScaleY(1f, openDuration).SetEase(Ease.OutBack));

        if (scannerCanvasGroup != null)
            seq.Join(scannerCanvasGroup.DOFade(1f, openDuration));

        if (scanLineImage != null)
        {
            scanLineImage.gameObject.SetActive(true);
            float panelHeight = scannerPanel.rect.height;
            scanLineImage.anchoredPosition = new Vector2(0, panelHeight * 0.5f);
            seq.Append(scanLineImage.DOAnchorPosY(-panelHeight * 0.5f, scanLineDuration).SetEase(Ease.Linear));
            seq.AppendCallback(() => { if (scanLineImage != null) scanLineImage.gameObject.SetActive(false); });
        }
    }

    private void CloseScanner()
    {
        if (scannerPanel == null) return;

        isOpen = false;
        currentTarget = null;

        Sequence seq = DOTween.Sequence();
        if (scannerCanvasGroup != null)
            seq.Append(scannerCanvasGroup.DOFade(0f, closeDuration));
        seq.Join(scannerPanel.DOScaleY(0f, closeDuration).SetEase(Ease.InBack));
        seq.OnComplete(() => { if (scannerPanel != null) scannerPanel.gameObject.SetActive(false); });
    }

    // =============================================================
    // テキスト生成
    // =============================================================

    private void SetScannerTexts(BattleUnit enemy)
    {
        if (enemy == null) return;

        Color typeColor = GetTypeColor(enemy.enemyType);

        if (nameText != null)
        {
            string lvStr = enemy.enemyLevel > 0 ? $"  Lv+{enemy.enemyLevel}" : "";
            nameText.text = $"[ SCAN ] {enemy.name.Replace("(Clone)", "")}{lvStr}";
        }

        if (typeText != null)
        {
            typeText.text = GetTypeLabel(enemy.enemyType);
            typeText.color = typeColor;
        }

        if (statsText != null)
        {
            string intervalStr = enemy.attackInterval <= 1
                ? "毎ターン攻撃"
                : $"{enemy.attackInterval}ターンごとに攻撃";

            statsText.text = $"HP {enemy.CurrentHP}/{enemy.maxHP}    ATK {enemy.attackPower}    {intervalStr}    EXP {enemy.expYield}    COIN {enemy.coinYield}";
        }

        if (patternText != null)
            patternText.text = GetPatternLabel(enemy.attackPattern, enemy.attackPower);

        if (adviceText != null)
        {
            adviceText.text = GetAdviceText(enemy);
            adviceText.color = typeColor;
        }

        if (weaknessText != null)
        {
            weaknessText.text = GetWeaknessText(enemy);
            weaknessText.color = new Color(1f, 0.92f, 0.35f, 1f);
            weaknessText.gameObject.SetActive(!string.IsNullOrEmpty(weaknessText.text));
        }
    }
    private string GetTypeLabel(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating: return "種別: 浮遊";
            case EnemyType.Armored: return "種別: 装甲";
            case EnemyType.Rushing: return "種別: 突撃";
            case EnemyType.Ranged: return "種別: 遠距離";
            default: return "種別: 通常";
        }
    }

    private Color GetTypeColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating: return floatingColor;
            case EnemyType.Armored: return armoredColor;
            case EnemyType.Rushing: return rushingColor;
            case EnemyType.Ranged: return rangedColor;
            default: return normalColor;
        }
    }

    private string GetPatternLabel(EnemyAttackPattern pattern, int atk)
    {
        switch (pattern)
        {
            case EnemyAttackPattern.HeavyHit:
                return $"行動: 重撃 — 1ターン溜め後に {atk * 2} ダメージ";
            case EnemyAttackPattern.MultiHit:
                return $"行動: 連撃 — {atk} × 2 回攻撃";
            case EnemyAttackPattern.SelfBuff:
                return $"行動: 自己修復 — ときどき {atk * 2} 回復";
            case EnemyAttackPattern.PanelCorrupt:
                return atk > 0
                    ? $"行動: 盤面汚染 — {atk} ダメージ + 腐敗化"
                    : "行動: 盤面汚染 — 盤面を腐敗化";
            default:
                return $"攻撃力: {atk}";
        }
    }

    private string GetAdviceText(BattleUnit enemy)
    {
        if (enemy == null) return "";

        string typeAdvice = GetTypeAdvice(enemy.enemyType);
        string patternAdvice = GetPatternAdvice(enemy.attackPattern);
        string gunAdvice = GetGunRecommendationText(enemy);

        string result = "";
        if (!string.IsNullOrEmpty(typeAdvice)) result = typeAdvice;
        if (!string.IsNullOrEmpty(patternAdvice)) result = string.IsNullOrEmpty(result) ? patternAdvice : $"{result}\n{patternAdvice}";
        if (!string.IsNullOrEmpty(gunAdvice)) result = string.IsNullOrEmpty(result) ? gunAdvice : $"{result}\n{gunAdvice}";
        return result;
    }

    private string GetTypeAdvice(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Floating: return "・近接ダメージ半減。銃で倒しやすい。";
            case EnemyType.Armored: return "・小ダメージを軽減。高威力攻撃が有効。";
            case EnemyType.Rushing: return "・毎ターン攻撃してくる。優先して倒したい。";
            case EnemyType.Ranged: return "・後衛から高圧をかける。早めに処理したい。";
            default: return "";
        }
    }

    private string GetWeaknessText(BattleUnit enemy)
    {
        if (enemy == null) return "";

        switch (enemy.enemyType)
        {
            case EnemyType.Floating:
                return "弱点: ライフル";
            case EnemyType.Ranged:
                return "弱点: ピストル / ライフル";
            case EnemyType.Armored:
                return "弱点: ショットガン / ライフル";
            case EnemyType.Rushing:
                return "弱点: ピストル / ショットガン";
            default:
                return "";
        }
    }

    private string GetGunRecommendationText(BattleUnit enemy)
    {
        if (enemy == null) return "";

        switch (enemy.enemyType)
        {
            case EnemyType.Floating:
                return "・推奨銃: ライフル";
            case EnemyType.Ranged:
                return "・推奨銃: ピストル / ライフル";
            case EnemyType.Armored:
                return "・推奨銃: ショットガン / ライフル";
            case EnemyType.Rushing:
                return "・推奨銃: ピストル / ショットガン";
            default:
                return "";
        }
    }

    private string GetPatternAdvice(EnemyAttackPattern pattern)
    {
        switch (pattern)
        {
            case EnemyAttackPattern.HeavyHit: return "・溜めターン中に倒せると安全。";
            case EnemyAttackPattern.MultiHit: return "・1ターンに複数回攻撃。HP管理に注意。";
            case EnemyAttackPattern.SelfBuff: return "・放置すると回復する。早めに押し切りたい。";
            case EnemyAttackPattern.PanelCorrupt: return "・盤面を腐敗パネルで汚染してくる。";
            default: return "";
        }
    }
}