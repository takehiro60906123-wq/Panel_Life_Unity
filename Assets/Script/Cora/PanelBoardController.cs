using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;



public class BoardRewardPanelCell
{
    public string rewardId;
    public string shortLabel;
    public string detailText;
    public Sprite iconSprite;
    public Color iconTint = Color.white;
    public int row;
    public int col;
    public PanelType originalType;
    public bool originalResonance;
}

public class PanelBoardController : MonoBehaviour
{
    [Header("盤面レイアウト設定")]
    public float cellSize = 150f;

    [Header("アイテム付きパネル演出")]
    [SerializeField] private Color normalPanelTint = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private BattleItemIconDatabase battleItemIconDatabase;
    [SerializeField] private Color attachedItemPanelTint = new Color(1f, 0.82f, 0.15f, 1f);

    [Header("パネル消去演出")]
    [SerializeField] private float tapFlashDuration = 0.04f;
    [SerializeField] private float tapScaleAmount = 1.15f;
    [SerializeField] private float chainStaggerDelay = 0.04f;
    [SerializeField] private float panelShrinkDuration = 0.15f;
    [SerializeField] private float boardShakeIntensity = 3f;
    [SerializeField] private int bigChainThreshold = 4;
    [SerializeField] private int maxChainThreshold = 5;
    [SerializeField] private GameObject panelBurstPrefab;
    [SerializeField] private GameObject swordPanelClearEffectPrefab;
    [SerializeField] private GameObject ammoPanelClearEffectPrefab;
    [SerializeField] private GameObject coinPanelClearEffectPrefab;
    [SerializeField] private GameObject healPanelClearEffectPrefab;
    [SerializeField] private GameObject levelUpPanelClearEffectPrefab;
    [SerializeField] private float chainPreviewScale = 1.10f;
    [SerializeField] private float chainPreviewJumpY = 10f;
    [SerializeField] private float chainPreviewFlashAlpha = 0.28f;
    [SerializeField] private float chainPreviewStagger = 0.015f;
    [Header("剣パネル演出")]
    [SerializeField] private Color swordAccentColor = new Color(1f, 0.82f, 0.35f, 1f);

    [SerializeField] private float swordTapScaleX = 1.16f;
    [SerializeField] private float swordTapScaleY = 0.92f;
    [SerializeField] private float swordTapRotateZ = -10f;

    [SerializeField] private float swordPreviewScaleX = 1.18f;
    [SerializeField] private float swordPreviewScaleY = 0.90f;
    [SerializeField] private float swordPreviewShiftX = 8f;

    [SerializeField] private float swordPreClearScaleX = 1.20f;
    [SerializeField] private float swordPreClearScaleY = 0.86f;
    [SerializeField] private float swordPreClearShiftX = 14f;
    [SerializeField] private float swordPreClearRotateZ = 14f;
    [SerializeField] private float swordPreClearDuration = 0.05f;
    [SerializeField] private float swordPreClearFlashAlpha = 0.45f;
    private static Sprite cachedWhiteSprite;
    private GameObject panelPrefab;
    private Transform boardParent;
    private List<PanelSetting> panelSettings;
    [Header("コインパネル設定")]
    [SerializeField] private bool excludeCoinPanelsFromRandomSpawn = true;

    [Header("腐敗パネル設定")]
    [SerializeField] private int maxCorruptPanelsOnBoard = 8;

    [Header("初期盤面パターン")]
    [SerializeField] private bool useCuratedOpeningLayouts = true;
    [SerializeField] private bool shuffleCuratedOpeningTypes = true;

    [Header("入場時パネル落下")]
    [SerializeField] private float introDropStartY = 280f;
    [SerializeField] private float introDropRowOffset = 42f;
    [SerializeField] private float introDropDuration = 0.34f;
    [SerializeField] private float introDropFadeDuration = 0.14f;
    [SerializeField] private float introDropRowStagger = 0.045f;
    [SerializeField] private float introDropColumnStagger = 0.012f;
    [SerializeField] private float introDropStartScale = 0.94f;
    [SerializeField] private float introDropLandingScale = 1.08f;
    [SerializeField] private float introDropFlashAlpha = 0.22f;
    [SerializeField] private float introDropImpactLeadTime = 0.05f;
    [SerializeField] private float introDropImpactShakeMultiplier = 1.16f;
    [SerializeField] private float introDropImpactShakeDuration = 0.10f;
    [SerializeField] private Ease introDropEase = Ease.OutBounce;

    [Header("報酬パネル出現")]
    [SerializeField] private float rewardRevealPreDelay = 0.08f;
    [SerializeField] private float rewardRevealStagger = 0.09f;
    [SerializeField] private float rewardRevealLift = 44f;
    [SerializeField] private float rewardRevealDuration = 0.28f;
    [SerializeField] private float rewardRevealFadeDuration = 0.16f;
    [SerializeField] private float rewardRevealStartScale = 0.72f;
    [SerializeField] private float rewardRevealOvershootScale = 1.14f;
    [SerializeField] private float rewardRevealFlashAlpha = 0.30f;
    [SerializeField] private Color rewardRevealFlashColor = new Color(1f, 0.90f, 0.48f, 1f);
    [SerializeField] private float rewardRevealBoardShakeLeadTime = 0.06f;
    [SerializeField] private float rewardRevealBoardShakeMultiplier = 1.22f;
    [SerializeField] private float rewardRevealBoardShakeDuration = 0.11f;

    [Header("パネル微呼吸")]
    [SerializeField] private bool enableIdleBreathing = true;
    [SerializeField][Range(1f, 1.05f)] private float idleBreathScaleMax = 1.02f;
    [SerializeField] private float idleBreathHalfDurationMin = 1.2f;
    [SerializeField] private float idleBreathHalfDurationMax = 1.6f;
    [SerializeField] private float idleBreathStartDelayMax = 1.2f;
    [SerializeField] private float idleBreathRandomScaleVariance = 0.003f;

    [Header("敵撃破時の盤面祝福")]
    [SerializeField] private float defeatCelebrateScale = 1.05f;
    [SerializeField] private float defeatCelebrateExpandDuration = 0.08f;
    [SerializeField] private float defeatCelebrateReturnDuration = 0.12f;
    [SerializeField] private float defeatCelebrateStagger = 0.008f;
    [SerializeField] private float defeatCelebrateFlashAlpha = 0.18f;

    [Header("危険敵撃破時の盤面強調")]
    [SerializeField] private Color dangerDefeatFlashColor = new Color(1f, 0.88f, 0.45f, 1f);
    [SerializeField] private float dangerDefeatCelebrateScale = 1.085f;
    [SerializeField] private float dangerDefeatExpandDuration = 0.09f;
    [SerializeField] private float dangerDefeatReturnDuration = 0.14f;
    [SerializeField] private float dangerDefeatStagger = 0.006f;
    [SerializeField] private float dangerDefeatFlashAlpha = 0.30f;
    [SerializeField] private float dangerDefeatShakeMultiplier = 1.35f;
    [SerializeField] private float dangerDefeatShakeDuration = 0.13f;

    [Header("回復到着時の盤面波紋")]
    [SerializeField] private Color healWaveColor = new Color(0.65f, 1f, 0.78f, 1f);
    [SerializeField] private float healWaveScale = 1.025f;
    [SerializeField] private float healWaveExpandDuration = 0.07f;
    [SerializeField] private float healWaveReturnDuration = 0.10f;
    [SerializeField] private float healWaveStagger = 0.004f;
    [SerializeField] private float healWaveFlashAlpha = 0.12f;

    [Header("EXP到着時の盤面チャージ")]
    [SerializeField] private Color expChargeColor = new Color(1f, 0.92f, 0.45f, 1f);
    [SerializeField] private float expChargeScale = 1.045f;
    [SerializeField] private float expChargeExpandDuration = 0.055f;
    [SerializeField] private float expChargeReturnDuration = 0.085f;
    [SerializeField] private float expChargeStagger = 0.02f;
    [SerializeField] private float expChargeFlashAlpha = 0.18f;

    [Header("共鳴パネル設定")]
    [SerializeField] private bool enableResonanceSwordPanels = true;
    [SerializeField] private int resonanceStartBattle = 10;
    [SerializeField] private int resonanceEndBattle = 18;
    [SerializeField, Range(0f, 1f)] private float resonanceChanceMidgame = 0.10f;
    [SerializeField, Range(0f, 1f)] private float resonanceChanceEndgame = 0.15f;
    [SerializeField] private Color resonanceBadgeColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color resonanceIconTint = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float resonanceBadgeSize = 26f;
    [SerializeField] private Vector2 resonanceBadgeOffset = new Vector2(-10f, -10f);
    [SerializeField] private float resonanceBadgePulseScale = 1.12f;
    [SerializeField] private float resonanceBadgePulseDuration = 0.55f;

    private static readonly string[] CuratedOpeningTemplates =
    {
        "224133|404113|400122|334422|310100|110133",
        "322033|324011|114333|333122|112120|442100",
        "422440|411400|314041|334001|224010|441110"
    };

    private bool boardPreparedForIntroDrop = false;

    private Action<int, int> onPanelClicked;
    private Func<int, int, bool> onSpecialPanelClicked;
    private BoardRewardPanelCell[,] rewardPanels;
    private static Font cachedRuntimeFont;

    private int rows;
    private int cols;

    private PanelType[,] gridData;
    private GameObject[,] panelObjects;
    private BattleItemData[,] attachedItems;
    private bool[,] resonanceFlags;

    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private StageFlowController stageFlowController;

    private void Awake()
    {
        if (battleUIController == null)
        {
            battleUIController = FindObjectOfType<BattleUIController>();
        }

        if (playerCombatController == null)
        {
            playerCombatController = FindObjectOfType<PlayerCombatController>();
        }

        if (stageFlowController == null)
        {
            stageFlowController = FindObjectOfType<StageFlowController>();
        }
    }

    public bool Initialize(
        GameObject panelPrefabValue,
        Transform boardParentValue,
        List<PanelSetting> panelSettingsValue,
        int rowsValue,
        int colsValue,
        Action<int, int> clickCallback)
    {
        panelPrefab = panelPrefabValue;
        boardParent = boardParentValue;
        panelSettings = panelSettingsValue;
        rows = rowsValue;
        cols = colsValue;
        onPanelClicked = clickCallback;

        if (panelPrefab == null)
        {
            Debug.LogError("PanelBoardController: panelPrefab が未設定です。");
            return false;
        }

        if (boardParent == null)
        {
            Debug.LogError("PanelBoardController: boardParent が未設定です。");
            return false;
        }

        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError("PanelBoardController: rows または cols が不正です。");
            return false;
        }

        gridData = new PanelType[rows, cols];
        panelObjects = new GameObject[rows, cols];
        attachedItems = new BattleItemData[rows, cols];
        resonanceFlags = new bool[rows, cols];
        rewardPanels = new BoardRewardPanelCell[rows, cols];

        return true;
    }

    public void SetSpecialPanelClickHandler(Func<int, int, bool> handler)
    {
        onSpecialPanelClicked = handler;
    }

    public void SetBattleItemIconDatabase(BattleItemIconDatabase database)
    {
        battleItemIconDatabase = database;
    }

    public GameObject GetPanelObject(int row, int col)
    {
        if (!IsInRange(row, col) || panelObjects == null) return null;
        return panelObjects[row, col];
    }

    public void GenerateBoard()
    {
        if (gridData == null || panelObjects == null) return;

        PanelType[,] openingLayout = TryBuildCuratedOpeningLayout();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject newPanel = Instantiate(panelPrefab, boardParent);
                newPanel.name = $"Panel_{r}_{c}";
                SetPanelBackgroundTransparent(newPanel);
                StartPanelIdleBreath(newPanel);

                PanelType panelType = openingLayout != null ? openingLayout[r, c] : GetRandomPanelType();
                gridData[r, c] = panelType;
                panelObjects[r, c] = newPanel;
                attachedItems[r, c] = null;
                resonanceFlags[r, c] = ShouldSpawnResonanceForPanel(panelType);

                UpdatePanelVisual(r, c);
                RefreshPanelHighlightVisual(r, c);

                int row = r;
                int col = c;

                Button btn = newPanel.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        RewardPanelPressHandler pressHandler = newPanel.GetComponent<RewardPanelPressHandler>();
                        if (pressHandler != null && pressHandler.ConsumeSuppressClick())
                        {
                            return;
                        }

                        if (onSpecialPanelClicked != null && onSpecialPanelClicked.Invoke(row, col))
                        {
                            return;
                        }

                        onPanelClicked?.Invoke(row, col);
                    });
                }
            }
        }
    }

    public void PrepareBoardForIntroDrop()
    {
        if (panelObjects == null || gridData == null)
        {
            return;
        }

        boardPreparedForIntroDrop = true;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;

                GameObject panelObj = panelObjects[r, c];
                if (panelObj == null) continue;

                Transform panelTransform = panelObj.transform;
                panelTransform.DOKill();
                panelTransform.localScale = Vector3.one * 0.94f;

                Transform iconTransform = panelTransform.Find("IconImage");
                if (iconTransform == null) continue;

                iconTransform.DOKill();

                Image img = iconTransform.GetComponent<Image>();
                if (img != null)
                {
                    img.DOKill();
                    Color color = img.color;
                    color.a = 0f;
                    img.color = color;
                }

                iconTransform.localScale = Vector3.one * introDropStartScale;
                iconTransform.localRotation = Quaternion.identity;
                iconTransform.localPosition = new Vector3(0f, ResolveIntroDropStartY(r), 0f);
            }
        }
    }

    public IEnumerator PlayIntroBoardDropRoutine()
    {
        if (panelObjects == null || gridData == null)
        {
            yield break;
        }

        if (!boardPreparedForIntroDrop)
        {
            yield break;
        }

        float longestTime = 0f;
        float columnCenter = (cols - 1) * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;

                GameObject panelObj = panelObjects[r, c];
                if (panelObj == null) continue;

                Transform panelTransform = panelObj.transform;
                Transform iconTransform = panelTransform.Find("IconImage");
                if (iconTransform == null) continue;

                panelTransform.DOKill();
                iconTransform.DOKill();

                Image img = iconTransform.GetComponent<Image>();
                if (img != null)
                {
                    img.DOKill();
                }

                float delay = (r * introDropRowStagger) + (Mathf.Abs(c - columnCenter) * introDropColumnStagger);
                float settleTime = delay + introDropDuration;
                if (settleTime > longestTime)
                {
                    longestTime = settleTime;
                }

                panelTransform.localScale = Vector3.one * 0.94f;
                iconTransform.localPosition = new Vector3(0f, ResolveIntroDropStartY(r), 0f);
                iconTransform.localScale = Vector3.one * introDropStartScale;
                iconTransform.localRotation = Quaternion.identity;

                iconTransform.DOLocalMoveY(0f, introDropDuration)
                    .SetDelay(delay)
                    .SetEase(introDropEase);

                Sequence panelScaleSeq = DOTween.Sequence();
                panelScaleSeq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                panelScaleSeq.SetDelay(delay);
                panelScaleSeq.Append(panelTransform.DOScale(introDropLandingScale, introDropDuration * 0.56f).SetEase(Ease.OutQuad));
                panelScaleSeq.Append(panelTransform.DOScale(1f, introDropDuration * 0.24f).SetEase(Ease.OutBack));
                panelScaleSeq.OnComplete(() =>
                {
                    if (panelObj != null)
                    {
                        StartPanelIdleBreath(panelObj);
                    }
                });

                Sequence iconScaleSeq = DOTween.Sequence();
                iconScaleSeq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                iconScaleSeq.SetDelay(delay);
                iconScaleSeq.Append(iconTransform.DOScale(1.06f, introDropDuration * 0.58f).SetEase(Ease.OutQuad));
                iconScaleSeq.Append(iconTransform.DOScale(1f, introDropDuration * 0.20f).SetEase(Ease.OutSine));

                if (img != null)
                {
                    Color targetColor = img.color;
                    targetColor.a = 1f;
                    img.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
                    img.DOFade(targetColor.a, introDropFadeDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad);
                }

                Image flash = GetOrCreateTapFlashFx(panelTransform);
                if (flash != null)
                {
                    flash.DOKill();
                    flash.enabled = true;
                    flash.gameObject.SetActive(true);
                    flash.color = new Color(1f, 0.96f, 0.72f, 0f);
                    flash.DOFade(introDropFlashAlpha, introDropDuration * 0.42f)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (flash != null)
                            {
                                flash.DOFade(0f, introDropDuration * 0.26f)
                                    .SetEase(Ease.OutSine)
                                    .OnComplete(() =>
                                    {
                                        if (flash != null)
                                        {
                                            flash.enabled = false;
                                        }
                                    });
                            }
                        });
                }
            }
        }

        boardPreparedForIntroDrop = false;

        if (longestTime > 0f)
        {
            float shakeTime = Mathf.Max(0f, longestTime - introDropImpactLeadTime);
            if (shakeTime > 0f)
            {
                yield return new WaitForSeconds(shakeTime);
            }

            if (introDropImpactShakeDuration > 0f)
            {
                PlayImpactShake(introDropImpactShakeMultiplier, introDropImpactShakeDuration);
            }

            float remaining = Mathf.Max(0f, longestTime - shakeTime);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining + 0.02f);
            }
        }
    }

    public PanelType GetPanelType(int row, int col)
    {
        if (!IsInRange(row, col)) return PanelType.None;
        if (HasRewardPanelAt(row, col)) return PanelType.None;
        return gridData[row, col];
    }

    public bool HasRewardPanelAt(int row, int col)
    {
        return IsInRange(row, col) && rewardPanels != null && rewardPanels[row, col] != null;
    }

    public BoardRewardPanelCell GetRewardPanelAt(int row, int col)
    {
        if (!HasRewardPanelAt(row, col)) return null;
        return rewardPanels[row, col];
    }

    public IEnumerator PlayRewardRevealRoutine(List<BoardRewardPanelCell> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            yield break;
        }

        if (panelObjects == null || rewardPanels == null)
        {
            yield break;
        }

        List<BoardRewardPanelCell> orderedRewards = new List<BoardRewardPanelCell>(rewards);
        Vector2 center = new Vector2((rows - 1) * 0.5f, (cols - 1) * 0.5f);
        orderedRewards.Sort((a, b) =>
        {
            float da = Vector2.SqrMagnitude(new Vector2(a.row, a.col) - center);
            float db = Vector2.SqrMagnitude(new Vector2(b.row, b.col) - center);
            int cmp = da.CompareTo(db);
            if (cmp != 0) return cmp;
            cmp = a.col.CompareTo(b.col);
            if (cmp != 0) return cmp;
            return a.row.CompareTo(b.row);
        });

        float longestTime = 0f;

        for (int i = 0; i < orderedRewards.Count; i++)
        {
            BoardRewardPanelCell reward = orderedRewards[i];
            if (reward == null || !IsInRange(reward.row, reward.col))
            {
                continue;
            }

            GameObject panelObj = panelObjects[reward.row, reward.col];
            if (panelObj == null)
            {
                continue;
            }

            Transform panelTransform = panelObj.transform;
            Transform iconTransform = panelTransform.Find("IconImage");
            Image img = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            panelTransform.DOKill();
            if (iconTransform != null)
            {
                iconTransform.DOKill();
            }
            if (img != null)
            {
                img.DOKill();
            }

            Vector3 basePos = panelTransform.localPosition;
            panelTransform.localPosition = basePos + Vector3.up * rewardRevealLift;
            panelTransform.localScale = Vector3.one * rewardRevealStartScale;

            if (iconTransform != null)
            {
                iconTransform.localPosition = Vector3.zero;
                iconTransform.localScale = Vector3.one * 0.86f;
                iconTransform.localRotation = Quaternion.identity;
            }

            if (img != null)
            {
                Color color = img.color;
                color.a = 0f;
                img.color = color;
            }

            float delay = rewardRevealPreDelay + (i * rewardRevealStagger);
            float settleTime = delay + rewardRevealDuration;
            if (settleTime > longestTime)
            {
                longestTime = settleTime;
            }

            Sequence panelSeq = DOTween.Sequence();
            panelSeq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
            panelSeq.SetDelay(delay);
            panelSeq.Append(panelTransform.DOLocalMove(basePos, rewardRevealDuration).SetEase(Ease.OutCubic));
            panelSeq.Join(panelTransform.DOScale(rewardRevealOvershootScale, rewardRevealDuration * 0.64f).SetEase(Ease.OutQuad));
            panelSeq.Append(panelTransform.DOScale(1f, rewardRevealDuration * 0.24f).SetEase(Ease.OutBack));
            panelSeq.OnComplete(() =>
            {
                if (panelObj != null)
                {
                    StartPanelIdleBreath(panelObj);
                }
            });

            if (iconTransform != null)
            {
                Sequence iconSeq = DOTween.Sequence();
                iconSeq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                iconSeq.SetDelay(delay);
                iconSeq.Append(iconTransform.DOScale(1.08f, rewardRevealDuration * 0.58f).SetEase(Ease.OutQuad));
                iconSeq.Append(iconTransform.DOScale(1f, rewardRevealDuration * 0.18f).SetEase(Ease.OutSine));
            }

            if (img != null)
            {
                img.DOFade(1f, rewardRevealFadeDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutQuad);
            }

            Image flash = GetOrCreateTapFlashFx(panelTransform);
            if (flash != null)
            {
                flash.DOKill();
                flash.enabled = true;
                flash.gameObject.SetActive(true);
                flash.color = new Color(rewardRevealFlashColor.r, rewardRevealFlashColor.g, rewardRevealFlashColor.b, 0f);
                flash.DOFade(rewardRevealFlashAlpha, rewardRevealDuration * 0.42f)
                    .SetDelay(delay)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.DOFade(0f, rewardRevealDuration * 0.22f)
                                .SetEase(Ease.OutSine)
                                .OnComplete(() =>
                                {
                                    if (flash != null)
                                    {
                                        flash.enabled = false;
                                    }
                                });
                        }
                    });
            }
        }

        if (longestTime > 0f)
        {
            float shakeTime = Mathf.Max(0f, longestTime - rewardRevealBoardShakeLeadTime);
            if (shakeTime > 0f)
            {
                yield return new WaitForSeconds(shakeTime);
            }

            if (rewardRevealBoardShakeDuration > 0f)
            {
                PlayImpactShake(rewardRevealBoardShakeMultiplier, rewardRevealBoardShakeDuration);
            }

            float remaining = Mathf.Max(0f, longestTime - shakeTime);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining + 0.02f);
            }
        }
    }

    public bool TryPlaceRewardPanels(List<BoardRewardPanelCell> rewards)
    {
        if (rewards == null || rewards.Count == 0) return false;
        if (gridData == null || panelObjects == null || rewardPanels == null) return false;

        ClearRewardPanels(true);

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;
                candidates.Add(new Vector2Int(r, c));
            }
        }

        if (candidates.Count < rewards.Count)
        {
            return false;
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2Int tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        Vector2 center = new Vector2((rows - 1) * 0.5f, (cols - 1) * 0.5f);
        candidates.Sort((a, b) =>
        {
            float da = Vector2.SqrMagnitude(new Vector2(a.x, a.y) - center);
            float db = Vector2.SqrMagnitude(new Vector2(b.x, b.y) - center);
            int cmp = da.CompareTo(db);
            if (cmp != 0) return cmp;

            cmp = a.x.CompareTo(b.x);
            if (cmp != 0) return cmp;

            return a.y.CompareTo(b.y);
        });

        for (int i = 0; i < rewards.Count; i++)
        {
            BoardRewardPanelCell cell = rewards[i];
            Vector2Int pos = candidates[i];
            cell.row = pos.x;
            cell.col = pos.y;
            cell.originalType = gridData[pos.x, pos.y];
            cell.originalResonance = resonanceFlags[pos.x, pos.y];

            rewardPanels[pos.x, pos.y] = cell;

            UpdatePanelVisual(pos.x, pos.y);
            RefreshPanelHighlightVisual(pos.x, pos.y);
            AttachRewardLongPressHandler(pos.x, pos.y);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj != null)
            {
                panelObj.transform.DOKill();
                panelObj.transform.localScale = Vector3.one;
                panelObj.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.24f, 6, 0.85f)
                    .SetUpdate(false);
            }
        }

        return true;
    }

    public void ClearRewardPanels(bool restoreUnderlying)
    {
        if (rewardPanels == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                BoardRewardPanelCell reward = rewardPanels[r, c];
                if (reward == null) continue;

                if (restoreUnderlying)
                {
                    gridData[r, c] = reward.originalType;
                    resonanceFlags[r, c] = reward.originalResonance;
                }

                rewardPanels[r, c] = null;
                DetachRewardLongPressHandler(r, c);
                UpdatePanelVisual(r, c);
            }
        }
    }

    public int GetPanelCount(PanelType targetType)
    {
        if (gridData == null) return 0;

        int count = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (HasRewardPanelAt(r, c)) continue;

                if (gridData[r, c] == targetType)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public void ClearAllAttachedItems()
    {
        if (attachedItems == null || panelObjects == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                attachedItems[r, c] = null;
                RefreshPanelHighlightVisual(r, c);
            }
        }
    }

    public bool TrySpawnAttachedItemPanel(float spawnChance)
    {
        if (gridData == null || attachedItems == null) return false;
        if (UnityEngine.Random.value > Mathf.Clamp01(spawnChance)) return false;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (HasRewardPanelAt(r, c)) continue;

                if (gridData[r, c] == PanelType.Sword
                    || gridData[r, c] == PanelType.Heal
                    || gridData[r, c] == PanelType.LvUp)
                {
                    candidates.Add(new Vector2Int(r, c));
                }
            }
        }

        if (candidates.Count == 0) return false;

        Vector2Int selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        BattleItemData item = CreateAttachedItemForPanelType(gridData[selected.x, selected.y]);
        if (item == null) return false;

        attachedItems[selected.x, selected.y] = item;
        RefreshPanelHighlightVisual(selected.x, selected.y);

        Debug.Log($"[PanelItem] Spawned at ({selected.x},{selected.y}) panel={gridData[selected.x, selected.y]} item={item.itemName}");

        return true;
    }

    public List<CollectedPanelItemInfo> ConsumeAttachedItems(List<Vector2Int> positions)
    {
        List<CollectedPanelItemInfo> result = new List<CollectedPanelItemInfo>();
        if (positions == null || positions.Count == 0) return result;
        if (attachedItems == null) return result;

        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>(positions);
        foreach (Vector2Int pos in uniquePositions)
        {
            if (!IsInRange(pos.x, pos.y)) continue;

            BattleItemData item = attachedItems[pos.x, pos.y];
            if (item == null) continue;

            result.Add(new CollectedPanelItemInfo
            {
                item = item,
                worldPosition = GetPanelWorldPosition(pos.x, pos.y)
            });

            attachedItems[pos.x, pos.y] = null;
            RefreshPanelHighlightVisual(pos.x, pos.y);
        }

        return result;
    }

    public int CollectAllPanelsOfType(PanelType targetType)
    {
        if (gridData == null || panelObjects == null) return 0;

        List<Vector2Int> positions = new List<Vector2Int>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (HasRewardPanelAt(r, c)) continue;

                if (gridData[r, c] == targetType)
                {
                    positions.Add(new Vector2Int(r, c));
                }
            }
        }

        if (positions.Count == 0)
        {
            return 0;
        }

        foreach (Vector2Int pos in positions)
        {
            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            resonanceFlags[pos.x, pos.y] = false;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform != null)
            {
                iconTransform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Image img = iconTransform.GetComponent<Image>();
                        if (img != null)
                        {
                            img.DOKill();
                            img.sprite = null;
                            img.color = Color.white;
                        }
                        iconTransform.localScale = Vector3.one;
                        iconTransform.localPosition = Vector3.zero;
                        iconTransform.localRotation = Quaternion.identity;
                    });
            }
        }

        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (this != null)
            {
                DropAndFillPanels();
            }
        });

        return positions.Count;
    }

    // ============================================
    // オーバーリンク対応: 連結探索結果
    // selected  = 実際に消すパネル（上限まで）
    // totalConnected = 同種連結の本当の総数
    // ============================================
    public struct ChainResult
    {
        public List<Vector2Int> selected;
        public int totalConnected;
    }

    public int GetResonanceBonusForSelection(PanelType clickedType, List<Vector2Int> selectedPanels)
    {
        if (!enableResonanceSwordPanels || clickedType != PanelType.Sword || selectedPanels == null || resonanceFlags == null)
        {
            return 0;
        }

        for (int i = 0; i < selectedPanels.Count; i++)
        {
            Vector2Int pos = selectedPanels[i];
            if (!IsInRange(pos.x, pos.y)) continue;
            if (resonanceFlags[pos.x, pos.y])
            {
                return 1;
            }
        }

        return 0;
    }

    public List<Vector2Int> CollectAdjacentCorruptPanels(List<Vector2Int> sourcePanels, PanelType sourceType)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (sourcePanels == null || sourcePanels.Count == 0)
        {
            return result;
        }

        if (sourceType == PanelType.Corrupt || sourceType == PanelType.None)
        {
            return result;
        }

        HashSet<Vector2Int> sourceSet = new HashSet<Vector2Int>(sourcePanels);
        HashSet<Vector2Int> corruptSet = new HashSet<Vector2Int>();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int pos in sourcePanels)
        {
            foreach (Vector2Int dir in directions)
            {
                int nr = pos.x + dir.x;
                int nc = pos.y + dir.y;

                if (!IsInRange(nr, nc)) continue;
                if (HasRewardPanelAt(nr, nc)) continue;

                Vector2Int next = new Vector2Int(nr, nc);
                if (sourceSet.Contains(next)) continue;

                if (gridData[nr, nc] == PanelType.Corrupt)
                {
                    corruptSet.Add(next);
                }
            }
        }

        result.AddRange(corruptSet);
        return result;
    }

    private int CountPanelsOfType(PanelType targetType)
    {
        if (gridData == null) return 0;

        int count = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (HasRewardPanelAt(r, c)) continue;
                if (gridData[r, c] == targetType)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public ChainResult FindChain(int startRow, int startCol, PanelType targetType)
    {
        ChainResult result = new ChainResult
        {
            selected = new List<Vector2Int>(),
            totalConnected = 0
        };

        if (!IsInRange(startRow, startCol)) return result;
        if (HasRewardPanelAt(startRow, startCol)) return result;

        // 腐敗パネルは連結させず、常に1枚ずつしか除去できない。
        if (targetType == PanelType.Corrupt)
        {
            result.selected.Add(new Vector2Int(startRow, startCol));
            result.totalConnected = 1;
            return result;
        }

        int maxLink = GetCurrentMaxLink();

        List<Vector2Int> allConnected = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int startNode = new Vector2Int(startRow, startCol);
        queue.Enqueue(startNode);
        visited.Add(startNode);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            allConnected.Add(current);

            foreach (Vector2Int dir in directions)
            {
                int nr = current.x + dir.x;
                int nc = current.y + dir.y;

                if (!IsInRange(nr, nc)) continue;
                if (HasRewardPanelAt(nr, nc)) continue;

                Vector2Int nextNode = new Vector2Int(nr, nc);
                if (!visited.Contains(nextNode) && gridData[nr, nc] == targetType)
                {
                    visited.Add(nextNode);
                    queue.Enqueue(nextNode);
                }
            }
        }

        int selectCount = Mathf.Min(allConnected.Count, maxLink);
        result.selected = allConnected.GetRange(0, selectCount);
        result.totalConnected = allConnected.Count;

        return result;
    }

    public List<Vector2Int> GetAdjacentLevelPanels(List<Vector2Int> attackChain)
    {
        HashSet<Vector2Int> levelPanels = new HashSet<Vector2Int>();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int pos in attackChain)
        {
            foreach (Vector2Int dir in directions)
            {
                int nr = pos.x + dir.x;
                int nc = pos.y + dir.y;

                if (!IsInRange(nr, nc)) continue;
                if (HasRewardPanelAt(nr, nc)) continue;

                if (gridData[nr, nc] == PanelType.LvUp)
                {
                    levelPanels.Add(new Vector2Int(nr, nc));
                }
            }
        }

        return new List<Vector2Int>(levelPanels);
    }

    public void ClearChainPanels(List<Vector2Int> chain)
    {
        if (chain == null || chain.Count == 0) return;

        foreach (Vector2Int pos in chain)
        {
            if (!IsInRange(pos.x, pos.y)) continue;

            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            resonanceFlags[pos.x, pos.y] = false;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform != null)
            {
                iconTransform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Image img = iconTransform.GetComponent<Image>();
                        if (img != null) img.sprite = null;
                        iconTransform.localScale = Vector3.one;
                    });
            }
        }
    }

    public void DropAndFillPanels()
    {
        if (HasAnyRewardPanels())
        {
            DropAndFillPanelsKeepingRewards();
            return;
        }

        for (int c = 0; c < cols; c++)
        {
            int writeRow = rows - 1;

            for (int r = rows - 1; r >= 0; r--)
            {
                if (gridData[r, c] != PanelType.None)
                {
                    if (r != writeRow)
                    {
                        gridData[writeRow, c] = gridData[r, c];
                        gridData[r, c] = PanelType.None;

                        attachedItems[writeRow, c] = attachedItems[r, c];
                        attachedItems[r, c] = null;

                        resonanceFlags[writeRow, c] = resonanceFlags[r, c];
                        resonanceFlags[r, c] = false;

                        Transform writeIcon = panelObjects[writeRow, c].transform.Find("IconImage");
                        Transform readIcon = panelObjects[r, c].transform.Find("IconImage");

                        if (writeIcon != null && readIcon != null)
                        {
                            Image writeImg = writeIcon.GetComponent<Image>();
                            Image readImg = readIcon.GetComponent<Image>();

                            if (writeImg != null && readImg != null)
                            {
                                writeIcon.DOKill();
                                readIcon.DOKill();
                                writeImg.DOKill();
                                readImg.DOKill();

                                writeImg.sprite = readImg.sprite;
                                writeImg.color = Color.white;
                                writeIcon.localScale = Vector3.one;
                                writeIcon.localRotation = Quaternion.identity;

                                readImg.sprite = null;
                                readImg.color = Color.white;
                                readIcon.localScale = Vector3.one;
                                readIcon.localPosition = Vector3.zero;
                                readIcon.localRotation = Quaternion.identity;

                                int dropDistance = writeRow - r;
                                writeIcon.localPosition = new Vector3(0, dropDistance * cellSize, 0);
                                writeIcon.DOLocalMoveY(0, 0.4f).SetEase(Ease.OutBounce);
                            }
                        }

                        RefreshPanelHighlightVisual(writeRow, c);
                        RefreshPanelHighlightVisual(r, c);
                    }

                    writeRow--;
                }
            }

            for (int r = writeRow; r >= 0; r--)
            {
                PanelType newType = GetRandomPanelType();
                gridData[r, c] = newType;
                attachedItems[r, c] = null;
                resonanceFlags[r, c] = ShouldSpawnResonanceForPanel(newType);

                Transform iconTransform = panelObjects[r, c].transform.Find("IconImage");
                if (iconTransform != null)
                {
                    iconTransform.DOKill();

                    Image img = iconTransform.GetComponent<Image>();
                    if (img != null)
                    {
                        img.DOKill();
                        img.sprite = GetSpriteForType(newType);
                        img.color = Color.white;
                    }

                    iconTransform.localScale = Vector3.one;
                    iconTransform.localRotation = Quaternion.identity;
                    iconTransform.localPosition = new Vector3(0, (r + 1) * cellSize, 0);

                    int dropOrder = writeRow - r;
                    iconTransform.DOLocalMoveY(0, 0.4f)
                        .SetDelay(0.08f * dropOrder)
                        .SetEase(Ease.OutBounce);
                }

                RefreshPanelHighlightVisual(r, c);
            }
        }
    }

    public Vector3 GetPanelWorldPosition(int row, int col)
    {
        if (!IsInRange(row, col)) return Vector3.zero;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return Vector3.zero;

        Transform uiTransform = panelObj.transform;
        Canvas canvas = boardParent.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            return uiTransform.position;
        }

        RectTransform rect = uiTransform as RectTransform;
        if (rect == null)
        {
            return uiTransform.position;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return uiTransform.position;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
        Vector3 world = new Vector3(screenPoint.x, screenPoint.y, Mathf.Abs(mainCam.transform.position.z));
        return mainCam.ScreenToWorldPoint(world);
    }

    private void UpdatePanelVisual(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        BoardRewardPanelCell reward = HasRewardPanelAt(row, col) ? rewardPanels[row, col] : null;
        if (icon != null)
        {
            icon.DOKill();

            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.DOKill();
                if (reward != null)
                {
                    img.sprite = reward.iconSprite != null ? reward.iconSprite : GetSpriteForType(reward.originalType);
                    img.color = reward.iconTint;
                }
                else
                {
                    img.sprite = GetSpriteForType(gridData[row, col]);
                    img.color = HasResonanceAt(row, col) ? resonanceIconTint : Color.white;
                }
            }

            icon.localScale = reward != null ? Vector3.one * 1.08f : Vector3.one;
            icon.localPosition = Vector3.zero;
            icon.localRotation = Quaternion.identity;
        }

        UpdateRewardBadgeVisual(panelObj.transform, reward);
        SetPanelBackgroundTransparent(panelObj);
        RefreshPanelHighlightVisual(row, col);
    }

    private void RefreshPanelHighlightVisual(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        bool hasItem = attachedItems != null && attachedItems[row, col] != null;
        bool hasReward = HasRewardPanelAt(row, col);

        // 親パネルは常に完全透明
        SetPanelBackgroundTransparent(panelObj);

        RefreshResonanceVisual(row, col, panelObj);

        // アイテム付き専用オーバーレイ
        Image itemFx = GetOrCreateItemPanelFx(panelObj.transform);
        if (itemFx != null)
        {
            itemFx.color = hasReward ? new Color(1f, 0.82f, 0.2f, 0.45f) : attachedItemPanelTint;
            itemFx.enabled = hasItem || hasReward;
            itemFx.gameObject.SetActive(hasItem || hasReward);
        }

        Transform badge = panelObj.transform.Find("ItemBadgeImage");
        if (badge == null)
        {
            badge = panelObj.transform.Find("ItemBadge");
        }

        if (badge != null)
        {
            badge.gameObject.SetActive(hasItem && !hasReward);
        }
    }

    private void StartPanelIdleBreath(GameObject panelObj)
    {
        if (panelObj == null) return;

        Transform panelTransform = panelObj.transform;
        panelTransform.DOKill();
        panelTransform.localScale = Vector3.one;

        if (!enableIdleBreathing)
        {
            return;
        }

        float scaleVariance = UnityEngine.Random.Range(-idleBreathRandomScaleVariance, idleBreathRandomScaleVariance);
        float targetScale = Mathf.Clamp(idleBreathScaleMax + scaleVariance, 1.005f, 1.05f);
        float halfDuration = UnityEngine.Random.Range(idleBreathHalfDurationMin, idleBreathHalfDurationMax);
        float startDelay = UnityEngine.Random.Range(0f, idleBreathStartDelayMax);

        Sequence seq = DOTween.Sequence();
        seq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
        seq.SetDelay(startDelay);
        seq.Append(panelTransform.DOScale(targetScale, halfDuration).SetEase(Ease.InOutSine));
        seq.Append(panelTransform.DOScale(1f, halfDuration).SetEase(Ease.InOutSine));
        seq.SetLoops(-1, LoopType.Restart);
    }



    private bool HasAnyRewardPanels()
    {
        if (rewardPanels == null) return false;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (rewardPanels[r, c] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void DropAndFillPanelsKeepingRewards()
    {
        for (int c = 0; c < cols; c++)
        {
            List<PanelType> types = new List<PanelType>();
            List<BattleItemData> items = new List<BattleItemData>();
            List<bool> resFlags = new List<bool>();

            for (int r = rows - 1; r >= 0; r--)
            {
                if (HasRewardPanelAt(r, c)) continue;
                if (gridData[r, c] == PanelType.None) continue;

                types.Add(gridData[r, c]);
                items.Add(attachedItems[r, c]);
                resFlags.Add(resonanceFlags[r, c]);
            }

            int readIndex = 0;

            for (int r = rows - 1; r >= 0; r--)
            {
                if (HasRewardPanelAt(r, c))
                {
                    UpdatePanelVisual(r, c);
                    continue;
                }

                if (readIndex < types.Count)
                {
                    gridData[r, c] = types[readIndex];
                    attachedItems[r, c] = items[readIndex];
                    resonanceFlags[r, c] = resFlags[readIndex];
                }
                else
                {
                    PanelType newType = GetRandomPanelType();
                    gridData[r, c] = newType;
                    attachedItems[r, c] = null;
                    resonanceFlags[r, c] = ShouldSpawnResonanceForPanel(newType);
                }

                readIndex++;
                UpdatePanelVisual(r, c);
                RefreshPanelHighlightVisual(r, c);
            }
        }
    }

    private bool HasResonanceAt(int row, int col)
    {
        if (HasRewardPanelAt(row, col))
        {
            return false;
        }

        return enableResonanceSwordPanels
            && resonanceFlags != null
            && IsInRange(row, col)
            && gridData[row, col] == PanelType.Sword
            && resonanceFlags[row, col];
    }

    private void RefreshResonanceVisual(int row, int col, GameObject panelObj)
    {
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.DOKill();
                iconImage.color = HasResonanceAt(row, col) ? resonanceIconTint : Color.white;
            }
        }

        Image badge = GetOrCreateResonanceBadge(panelObj.transform);
        if (badge == null) return;

        bool active = HasResonanceAt(row, col);
        badge.DOKill();
        badge.transform.DOKill();

        if (!active)
        {
            badge.color = new Color(resonanceBadgeColor.r, resonanceBadgeColor.g, resonanceBadgeColor.b, 0f);
            badge.gameObject.SetActive(false);
            badge.transform.localScale = Vector3.one;
            badge.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return;
        }

        badge.gameObject.SetActive(true);
        badge.color = resonanceBadgeColor;
        badge.transform.localScale = Vector3.one;
        badge.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        badge.DOFade(Mathf.Clamp01(resonanceBadgeColor.a * 0.55f), resonanceBadgePulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        badge.transform.DOScale(resonanceBadgePulseScale, resonanceBadgePulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private Image GetOrCreateResonanceBadge(Transform panelTransform)
    {
        if (panelTransform == null) return null;

        Transform badge = panelTransform.Find("ResonanceBadgeImage");
        if (badge == null)
        {
            badge = panelTransform.Find("ResonanceBadge");
        }

        if (badge == null)
        {
            GameObject go = new GameObject("ResonanceBadgeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badge = go.transform;
            badge.SetParent(panelTransform, false);
            badge.SetSiblingIndex(panelTransform.childCount - 1);

            RectTransform rt = badge as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.one;
                rt.anchorMax = Vector2.one;
                rt.pivot = Vector2.one;
                rt.anchoredPosition = resonanceBadgeOffset;
                rt.sizeDelta = new Vector2(resonanceBadgeSize, resonanceBadgeSize);
                rt.localScale = Vector3.one;
            }

            Image img = badge.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);
                img.enabled = true;
            }
        }

        return badge.GetComponent<Image>();
    }

    private Image GetOrCreateItemPanelFx(Transform panelTransform)
    {
        if (panelTransform == null) return null;

        Transform fx = panelTransform.Find("ItemPanelFx");
        if (fx == null)
        {
            GameObject go = new GameObject("ItemPanelFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fx = go.transform;
            fx.SetParent(panelTransform, false);
            fx.SetSiblingIndex(0); // IconImage より後ろ

            RectTransform rt = fx as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Image img = fx.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.enabled = false;
            }
        }

        return fx.GetComponent<Image>();
    }

    private Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        Texture2D tex = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return cachedWhiteSprite;
    }

    private BattleItemData CreatePresetWithIcon(BattleItemType itemType)
    {
        if (battleItemIconDatabase != null)
        {
            BattleItemData itemWithIcon = battleItemIconDatabase.CreatePreset(itemType);
            if (itemWithIcon != null)
            {
                return itemWithIcon;
            }
        }

        return BattleItemData.CreatePreset(itemType);
    }

    private BattleItemData CreateAttachedItemForPanelType(PanelType panelType)
    {
        int roll = UnityEngine.Random.Range(0, 100);

        switch (panelType)
        {
            case PanelType.Sword:
                return CreatePresetWithIcon(roll < 60
                    ? BattleItemType.ShockCanister
                    : BattleItemType.ActivationCell);

            case PanelType.Heal:
                return CreatePresetWithIcon(roll < 75
                    ? BattleItemType.FieldBandage
                    : BattleItemType.ActivationCell);

            case PanelType.LvUp:
                return CreatePresetWithIcon(roll < 50
                    ? BattleItemType.ActivationCell
                    : BattleItemType.ShockCanister);
        }

        return null;
    }

    private PanelType[,] TryBuildCuratedOpeningLayout()
    {
        if (!useCuratedOpeningLayouts)
        {
            return null;
        }

        if (rows != 6 || cols != 6)
        {
            return null;
        }

        string[] templates =
        {
            // 中央に役割ブロックをまとめ、周囲に帯を置く型。
            "HHEEAA/HSSSAA/EHSSAE/AEHHAE/AAHHSE/EEHHSS",

            // 縦レーン感を強めた型。役割が列で見える。
            "HASEEH/HASEEH/SEAHHS/SEAHHS/EHSAAE/EHSAAE",

            // 中央に剣と弾薬の塊、上下に回復とEXPの帯を置く型。
            "HHEEHH/ASSSAE/ASSHAE/EAHHSE/EAASSE/HHEEHH",

            // 左右に細い帯、中央に2x2塊を段で重ねる型。
            "HEAASH/HEAASH/SSAHEE/SSAHEE/AASEHH/AASEHH",

            // 盤面中央へ視線が集まるよう、中央2列を強く見せる型。
            "HAEASH/HAEASH/ESSHAE/ESSHAE/AHEEAS/AHEEAS"
        };

        string picked = templates[UnityEngine.Random.Range(0, templates.Length)];
        return BuildLayoutFromTemplate(picked);
    }

    private PanelType[,] BuildLayoutFromTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return null;
        }

        string[] rowTokens = template.Split('/');
        if (rowTokens.Length != rows)
        {
            Debug.LogWarning($"PanelBoardController: 初期盤面テンプレの行数が一致しません: {template}");
            return null;
        }

        PanelType[,] layout = new PanelType[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            string rowToken = rowTokens[r];
            if (string.IsNullOrEmpty(rowToken) || rowToken.Length != cols)
            {
                Debug.LogWarning($"PanelBoardController: 初期盤面テンプレの列数が一致しません: {template}");
                return null;
            }

            for (int c = 0; c < cols; c++)
            {
                layout[r, c] = GetCuratedOpeningPanelType(rowToken[c]);
            }
        }

        return layout;
    }

    private PanelType GetCuratedOpeningPanelType(char token)
    {
        switch (char.ToUpperInvariant(token))
        {
            case 'S': return PanelType.Sword;
            case 'A': return PanelType.Ammo;
            case 'H': return PanelType.Heal;
            case 'E': return PanelType.LvUp;
            default: return GetRandomPanelType();
        }
    }

    private float ResolveIntroDropStartY(int row)
    {
        return introDropStartY + ((rows - 1 - row) * introDropRowOffset);
    }

    private PanelType GetRandomPanelType()
    {
        int totalWeight = 0;

        if (panelSettings != null)
        {
            foreach (var setting in panelSettings)
            {
                if (setting == null) continue;
                if (excludeCoinPanelsFromRandomSpawn && setting.type == PanelType.Coin) continue;
                if (setting.weight <= 0) continue;
                totalWeight += setting.weight;
            }
        }

        if (totalWeight <= 0) return PanelType.Sword;

        int randomVal = UnityEngine.Random.Range(0, totalWeight);

        foreach (var setting in panelSettings)
        {
            if (setting == null) continue;
            if (excludeCoinPanelsFromRandomSpawn && setting.type == PanelType.Coin) continue;
            if (setting.weight <= 0) continue;

            randomVal -= setting.weight;
            if (randomVal < 0) return setting.type;
        }

        return PanelType.Sword;
    }

    private void UpdateRewardBadgeVisual(Transform parent, BoardRewardPanelCell reward)
    {
        Transform badgeTransform = parent.Find("RewardBadgeText");
        if (reward == null)
        {
            if (badgeTransform != null)
            {
                badgeTransform.gameObject.SetActive(false);
            }
            return;
        }

        Text text = badgeTransform != null ? badgeTransform.GetComponent<Text>() : null;
        if (text == null)
        {
            GameObject go = new GameObject("RewardBadgeText", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(110f, 28f);
            text = go.AddComponent<Text>();
            text.font = GetRuntimeFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 22;
            text.raycastTarget = false;
            badgeTransform = go.transform;
        }

        text.text = reward.shortLabel;
        text.color = Color.white;
        badgeTransform.gameObject.SetActive(true);
    }

    private Font GetRuntimeFont()
    {
        if (cachedRuntimeFont == null)
        {
            cachedRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return cachedRuntimeFont;
    }

    private void AttachRewardLongPressHandler(int row, int col)
    {
        GameObject panelObj = GetPanelObject(row, col);
        if (panelObj == null) return;

        RewardPanelPressHandler handler = panelObj.GetComponent<RewardPanelPressHandler>();
        if (handler == null)
        {
            handler = panelObj.AddComponent<RewardPanelPressHandler>();
        }

        handler.Configure(row, col, onSpecialPanelLongPressed, onSpecialPanelLongPressReleased);
    }

    private void DetachRewardLongPressHandler(int row, int col)
    {
        GameObject panelObj = GetPanelObject(row, col);
        if (panelObj == null) return;

        RewardPanelPressHandler handler = panelObj.GetComponent<RewardPanelPressHandler>();
        if (handler != null)
        {
            handler.Clear();
        }
    }

    private Action<int, int> onSpecialPanelLongPressed;
    private Action<int, int> onSpecialPanelLongPressReleased;

    public void SetSpecialPanelLongPressHandler(Action<int, int> handler)
    {
        onSpecialPanelLongPressed = handler;
        onSpecialPanelLongPressReleased = null;
    }

    public void SetSpecialPanelLongPressHandlers(Action<int, int> onPressed, Action<int, int> onReleased)
    {
        onSpecialPanelLongPressed = onPressed;
        onSpecialPanelLongPressReleased = onReleased;
    }

    public Sprite GetDisplaySpriteForPanelType(PanelType type)
    {
        return GetSpriteForType(type);
    }

    private Sprite GetSpriteForType(PanelType type)
    {
        if (panelSettings != null)
        {
            foreach (var setting in panelSettings)
            {
                if (setting.type == type) return setting.panelImage;
            }
        }

        return null;
    }

    private bool IsInRange(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }

    private int GetCurrentMaxLink()
    {
        if (playerCombatController == null)
            return 3;

        return playerCombatController.GetMaxLink();
    }

    private bool ShouldSpawnResonanceForPanel(PanelType type)
    {
        if (!enableResonanceSwordPanels || type != PanelType.Sword)
        {
            return false;
        }

        float chance = GetResonanceSpawnChanceForCurrentBattle();
        return chance > 0f && UnityEngine.Random.value < chance;
    }

    private float GetResonanceSpawnChanceForCurrentBattle()
    {
        int battleNumber = 1;
        if (stageFlowController != null)
        {
            battleNumber = Mathf.Max(1, stageFlowController.DefeatedEnemyCount + 1);
        }

        if (battleNumber < resonanceStartBattle)
        {
            return 0f;
        }

        if (battleNumber >= resonanceEndBattle)
        {
            return resonanceChanceEndgame;
        }

        return resonanceChanceMidgame;
    }

    /// <summary>
    /// 盤面のランダムな位置にパネルを強制配置する（演出つき）。
    /// PanelCorrupt（盤面汚染）攻撃で使用。
    /// </summary>
    public int ForceSetRandomPanels(PanelType type, int count)
    {
        if (gridData == null || panelObjects == null) return 0;

        count = Mathf.Max(0, count);
        if (count <= 0) return 0;

        if (type == PanelType.Corrupt)
        {
            int activeCorrupt = CountPanelsOfType(PanelType.Corrupt);
            int remainingCapacity = Mathf.Max(0, maxCorruptPanelsOnBoard - activeCorrupt);
            count = Mathf.Min(count, remainingCapacity);

            if (count <= 0)
            {
                return 0;
            }
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (HasRewardPanelAt(r, c)) continue;
                if (gridData[r, c] == type) continue;
                candidates.Add(new Vector2Int(r, c));
            }
        }

        if (candidates.Count == 0) return 0;

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        int placed = 0;
        for (int i = 0; i < Mathf.Min(count, candidates.Count); i++)
        {
            Vector2Int pos = candidates[i];
            gridData[pos.x, pos.y] = type;
            resonanceFlags[pos.x, pos.y] = ShouldSpawnResonanceForPanel(type);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj != null)
            {
                Transform icon = panelObj.transform.Find("IconImage");
                if (icon != null)
                {
                    Image img = icon.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = GetSpriteForType(type);
                        img.color = resonanceFlags[pos.x, pos.y] ? resonanceIconTint : Color.white;
                    }

                    icon.localScale = Vector3.one * 0.1f;
                    icon.localPosition = Vector3.zero;
                    icon.localRotation = Quaternion.identity;

                    SetPanelBackgroundTransparent(panelObj);

                    icon.DOScale(Vector3.one, 0.35f)
                        .SetDelay(0.15f * i)
                        .SetEase(Ease.OutBack);
                }
            }

            RefreshPanelHighlightVisual(pos.x, pos.y);
            placed++;
        }

        return placed;
    }

    private void SetPanelBackgroundTransparent(GameObject panelObj)
    {
        if (panelObj == null) return;

        Image bg = panelObj.GetComponent<Image>();
        if (bg != null)
        {
            Color c = bg.color;
            c.a = 0f;
            bg.color = c;
        }
    }

    public void PlayItemPanelCollectEffect(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon == null) return;

        Image itemFx = GetOrCreateItemPanelFx(panelObj.transform);

        if (itemFx != null)
        {
            itemFx.color = new Color(1f, 0.85f, 0.3f, 0f);

            itemFx
                .DOFade(0.9f, 0.12f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    itemFx.DOFade(0f, 0.25f);
                });
        }

        icon
            .DOScale(1.35f, 0.12f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                icon.DOScale(1f, 0.1f);
            });

        icon
            .DOLocalMoveY(icon.localPosition.y + 22f, 0.15f)
            .SetLoops(2, LoopType.Yoyo);
    }


    private void ResetIconTransform(Transform icon)
    {
        if (icon == null) return;

        icon.DOKill();
        icon.localScale = Vector3.one;
        icon.localPosition = Vector3.zero;
        icon.localRotation = Quaternion.identity;
    }

    // ============================================
    // タップ瞬間フィードバック
    // ============================================

    public void PlayTapFeedback(int row, int col, PanelType panelType)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            ResetIconTransform(icon);

            if (panelType == PanelType.Sword)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(new Vector3(swordTapScaleX, swordTapScaleY, 1f), 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMoveX(6f, 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMoveX(0f, 0.08f).SetEase(Ease.InOutQuad));
            }
            else
            {
                icon.DOScale(tapScaleAmount, 0.06f).SetEase(Ease.OutQuad);
            }
        }

        Image flash = GetOrCreateTapFlashFx(panelObj.transform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);

            Color flashColor = panelType == PanelType.Sword
                ? new Color(swordAccentColor.r, swordAccentColor.g, swordAccentColor.b, 0f)
                : new Color(1f, 1f, 1f, 0f);

            flash.color = flashColor;

            float targetAlpha = panelType == PanelType.Sword ? 0.55f : 0.7f;

            flash.DOFade(targetAlpha, tapFlashDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.06f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }

    public void PlayChainPreviewFeedback(List<Vector2Int> chain, PanelType panelType)
    {
        if (chain == null || chain.Count == 0) return;

        Color accent = GetChainPreviewColor(panelType);

        for (int i = 0; i < chain.Count; i++)
        {
            Vector2Int pos = chain[i];
            float delay = i * chainPreviewStagger;

            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null) return;
                PlaySingleChainPreview(pos.x, pos.y, panelType, accent);
            });
        }
    }

    private Color GetChainPreviewColor(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.Sword:
                return new Color(1f, 0.82f, 0.35f, 1f);

            case PanelType.Ammo:
                return new Color(0.45f, 0.9f, 1f, 1f);

            case PanelType.Heal:
                return new Color(0.45f, 1f, 0.55f, 1f);

            case PanelType.Coin:
                return new Color(1f, 0.9f, 0.25f, 1f);

            case PanelType.LvUp:
                return new Color(0.75f, 0.55f, 1f, 1f);

            case PanelType.Corrupt:
                return new Color(0.48f, 0.92f, 0.62f, 1f);

            default:
                return Color.white;
        }
    }

    private void PlaySingleChainPreview(int row, int col, PanelType panelType, Color flashColor)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            ResetIconTransform(icon);

            Vector3 baseScale = Vector3.one;
            Vector3 basePos = Vector3.zero;

            if (panelType == PanelType.Sword)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(new Vector3(swordPreviewScaleX, swordPreviewScaleY, 1f), 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMove(basePos + new Vector3(swordPreviewShiftX, chainPreviewJumpY, 0f), 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(baseScale, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMove(basePos, 0.08f).SetEase(Ease.InOutQuad));
            }
            else
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(chainPreviewScale, 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMoveY(basePos.y + chainPreviewJumpY, 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(baseScale, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMoveY(basePos.y, 0.08f).SetEase(Ease.InOutQuad));
            }
        }

        Image flash = GetOrCreateTapFlashFx(panelObj.transform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

            float alpha = panelType == PanelType.Sword
                ? Mathf.Max(chainPreviewFlashAlpha, 0.35f)
                : chainPreviewFlashAlpha;

            flash.DOFade(alpha, 0.05f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.08f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }

    private void PlaySwordPreClearFeedback(Transform iconTransform, Transform panelTransform)
    {
        if (iconTransform == null) return;

        ResetIconTransform(iconTransform);

        Vector3 baseScale = Vector3.one;
        Vector3 basePos = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(iconTransform.DOScale(new Vector3(swordPreClearScaleX, swordPreClearScaleY, 1f), swordPreClearDuration).SetEase(Ease.OutQuad));
        seq.Join(iconTransform.DOLocalMove(basePos + new Vector3(swordPreClearShiftX, 0f, 0f), swordPreClearDuration).SetEase(Ease.OutQuad));
        seq.Append(iconTransform.DOScale(baseScale, 0.04f).SetEase(Ease.InQuad));
        seq.Join(iconTransform.DOLocalMove(basePos, 0.04f).SetEase(Ease.InQuad));

        Image flash = GetOrCreateTapFlashFx(panelTransform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);
            flash.color = new Color(swordAccentColor.r, swordAccentColor.g, swordAccentColor.b, 0f);

            flash.DOFade(swordPreClearFlashAlpha, swordPreClearDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.05f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }
    public void PlayImpactShake(float multiplier = 1f, float duration = 0.10f)
    {
        if (boardParent == null) return;

        boardParent.DOKill();
        boardParent.DOShakePosition(
            duration,
            boardShakeIntensity * multiplier,
            20,
            90f,
            false,
            true
        );
    }

    public void PlayDefeatCelebration()
    {
        PlayDefeatCelebrationInternal(
            Color.white,
            defeatCelebrateScale,
            defeatCelebrateExpandDuration,
            defeatCelebrateReturnDuration,
            defeatCelebrateStagger,
            defeatCelebrateFlashAlpha,
            1f,
            0f);
    }

    public void PlayDangerDefeatCelebration()
    {
        PlayDefeatCelebrationInternal(
            dangerDefeatFlashColor,
            dangerDefeatCelebrateScale,
            dangerDefeatExpandDuration,
            dangerDefeatReturnDuration,
            dangerDefeatStagger,
            dangerDefeatFlashAlpha,
            dangerDefeatShakeMultiplier,
            dangerDefeatShakeDuration);
    }

    private void PlayDefeatCelebrationInternal(
        Color flashColor,
        float celebrateScale,
        float expandDuration,
        float returnDuration,
        float stagger,
        float flashAlpha,
        float shakeMultiplier,
        float shakeDuration)
    {
        if (panelObjects == null || gridData == null) return;

        if (shakeDuration > 0f)
        {
            PlayImpactShake(shakeMultiplier, shakeDuration);
        }

        int order = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;

                GameObject panelObj = panelObjects[r, c];
                if (panelObj == null) continue;

                float delay = order * stagger;
                order++;

                Transform panelTransform = panelObj.transform;
                panelTransform.DOKill();
                Vector3 startScale = panelTransform.localScale;

                Sequence seq = DOTween.Sequence();
                seq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                seq.SetDelay(delay);
                seq.Append(panelTransform.DOScale(startScale * celebrateScale, expandDuration).SetEase(Ease.OutQuad));
                seq.Append(panelTransform.DOScale(startScale, returnDuration).SetEase(Ease.OutBack));
                seq.OnComplete(() =>
                {
                    if (panelObj != null)
                    {
                        StartPanelIdleBreath(panelObj);
                    }
                });

                Image flash = GetOrCreateTapFlashFx(panelTransform);
                if (flash != null)
                {
                    flash.DOKill();
                    flash.enabled = true;
                    flash.gameObject.SetActive(true);
                    flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
                    flash.DOFade(flashAlpha, expandDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (flash != null)
                            {
                                flash.DOFade(0f, returnDuration).OnComplete(() =>
                                {
                                    if (flash != null)
                                    {
                                        flash.enabled = false;
                                    }
                                });
                            }
                        });
                }
            }
        }
    }

    public void PlayHealWaveFeedback()
    {
        if (panelObjects == null || gridData == null) return;

        int order = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;

                GameObject panelObj = panelObjects[r, c];
                if (panelObj == null) continue;

                float delay = order * healWaveStagger;
                order++;

                Transform panelTransform = panelObj.transform;
                panelTransform.DOKill();
                Vector3 startScale = panelTransform.localScale;

                Sequence seq = DOTween.Sequence();
                seq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                seq.SetDelay(delay);
                seq.Append(panelTransform.DOScale(startScale * healWaveScale, healWaveExpandDuration).SetEase(Ease.OutQuad));
                seq.Append(panelTransform.DOScale(startScale, healWaveReturnDuration).SetEase(Ease.OutSine));
                seq.OnComplete(() =>
                {
                    if (panelObj != null)
                    {
                        StartPanelIdleBreath(panelObj);
                    }
                });

                Image flash = GetOrCreateTapFlashFx(panelTransform);
                if (flash != null)
                {
                    flash.DOKill();
                    flash.enabled = true;
                    flash.gameObject.SetActive(true);
                    flash.color = new Color(healWaveColor.r, healWaveColor.g, healWaveColor.b, 0f);
                    flash.DOFade(healWaveFlashAlpha, healWaveExpandDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (flash != null)
                            {
                                flash.DOFade(0f, healWaveReturnDuration).OnComplete(() =>
                                {
                                    if (flash != null)
                                    {
                                        flash.enabled = false;
                                    }
                                });
                            }
                        });
                }
            }
        }
    }

    public void PlayExpChargeFeedback()
    {
        if (panelObjects == null || gridData == null) return;

        int order = 0;
        for (int r = rows - 1; r >= 0; r--)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.None) continue;

                GameObject panelObj = panelObjects[r, c];
                if (panelObj == null) continue;

                float delay = order * expChargeStagger;
                order++;

                Transform panelTransform = panelObj.transform;
                panelTransform.DOKill();
                Vector3 startScale = panelTransform.localScale;

                Sequence seq = DOTween.Sequence();
                seq.SetLink(panelObj, LinkBehaviour.KillOnDestroy);
                seq.SetDelay(delay);
                seq.Append(panelTransform.DOScale(startScale * expChargeScale, expChargeExpandDuration).SetEase(Ease.OutQuad));
                seq.Append(panelTransform.DOScale(startScale, expChargeReturnDuration).SetEase(Ease.OutSine));
                seq.OnComplete(() =>
                {
                    if (panelObj != null)
                    {
                        StartPanelIdleBreath(panelObj);
                    }
                });

                Image flash = GetOrCreateTapFlashFx(panelTransform);
                if (flash != null)
                {
                    flash.DOKill();
                    flash.enabled = true;
                    flash.gameObject.SetActive(true);
                    flash.color = new Color(expChargeColor.r, expChargeColor.g, expChargeColor.b, 0f);
                    flash.DOFade(expChargeFlashAlpha, expChargeExpandDuration)
                        .SetDelay(delay)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (flash != null)
                            {
                                flash.DOFade(0f, expChargeReturnDuration).OnComplete(() =>
                                {
                                    if (flash != null)
                                    {
                                        flash.enabled = false;
                                    }
                                });
                            }
                        });
                }
            }
        }
    }

    private Image GetOrCreateTapFlashFx(Transform panelTransform)
    {
        if (panelTransform == null) return null;

        Transform fx = panelTransform.Find("TapFlashFx");
        if (fx == null)
        {
            GameObject go = new GameObject("TapFlashFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fx = go.transform;
            fx.SetParent(panelTransform, false);
            fx.SetAsLastSibling();

            RectTransform rt = fx as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Image img = fx.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.enabled = false;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        return fx.GetComponent<Image>();
    }

    private GameObject ResolvePanelClearEffectPrefab(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.Sword:
                return swordPanelClearEffectPrefab != null ? swordPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Ammo:
                return ammoPanelClearEffectPrefab != null ? ammoPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Coin:
                return coinPanelClearEffectPrefab != null ? coinPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Heal:
                return healPanelClearEffectPrefab != null ? healPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.LvUp:
                return levelUpPanelClearEffectPrefab != null ? levelUpPanelClearEffectPrefab : panelBurstPrefab;
            default:
                return panelBurstPrefab;
        }
    }

    // ============================================
    // 波紋ディレイ付き消去（コルーチン版）
    // ClearChainPanels の演出強化版。落下補充を内包する。
    // 既存の ClearChainPanels はそのまま残す（一括消去用）。
    // ============================================

    public IEnumerator ClearChainPanelsAnimated(List<Vector2Int> chain, PanelType sourceType, Action onComplete = null)
    {
        if (chain == null || chain.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        int chainSize = chain.Count;

        for (int i = 0; i < chainSize; i++)
        {
            Vector2Int pos = chain[i];
            if (!IsInRange(pos.x, pos.y)) continue;

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform == null) continue;

            Image img = iconTransform.GetComponent<Image>();

            if (sourceType == PanelType.Sword)
            {
                PlaySwordPreClearFeedback(iconTransform, panelObj.transform);
                yield return new WaitForSeconds(0.02f);
            }

            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            resonanceFlags[pos.x, pos.y] = false;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            if (img != null)
            {
                img.DOColor(Color.white, 0.04f).SetEase(Ease.OutQuad);
            }

            iconTransform.DOScale(Vector3.zero, panelShrinkDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (img != null)
                    {
                        img.sprite = null;
                        img.color = Color.white;
                    }
                    iconTransform.localScale = Vector3.one;
                    iconTransform.localPosition = Vector3.zero;
                    iconTransform.localRotation = Quaternion.identity;
                });

            if (img != null)
            {
                img.DOFade(0f, panelShrinkDuration * 0.8f);
            }

            GameObject clearEffectPrefab = ResolvePanelClearEffectPrefab(sourceType);
            if (clearEffectPrefab != null)
            {
                Vector3 burstPos = GetPanelWorldPosition(pos.x, pos.y);
                GameObject burst = Instantiate(clearEffectPrefab, burstPos, Quaternion.identity);
                Destroy(burst, 1.0f);
            }

            if (i < chainSize - 1)
            {
                yield return new WaitForSeconds(chainStaggerDelay);
            }
        }

        yield return new WaitForSeconds(panelShrinkDuration);

        if (chainSize >= bigChainThreshold && boardParent != null)
        {
            float intensity = chainSize >= maxChainThreshold
                ? boardShakeIntensity * 1.5f
                : boardShakeIntensity;
            boardParent.DOShakePosition(0.15f, intensity, 20, 90f, false, true);
        }

        DropAndFillPanels();

        onComplete?.Invoke();
    }
}