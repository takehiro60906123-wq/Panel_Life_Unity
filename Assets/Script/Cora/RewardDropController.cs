using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardDropController : MonoBehaviour
{
    [Header("提出版固定報酬タイミング")]
    private const int GuaranteedSwordBattleNumber = 5;
    private const int GuaranteedShotgunBattleNumber = 10;
    private const int GuaranteedGreatSwordBattleNumber = 15;
    private const int GuaranteedRifleBattleNumber = 20;

    [SerializeField] private Color promptTextColor = new Color(1f, 0.9f, 0.35f, 1f);

    [Header("報酬アイコン登録")]
    [SerializeField] private Sprite swordRewardIcon;
    [SerializeField] private Sprite greatSwordRewardIcon;
    [SerializeField] private Sprite pistolRewardIcon;
    [SerializeField] private Sprite machineGunRewardIcon;
    [SerializeField] private Sprite shotgunRewardIcon;
    [SerializeField] private Sprite rifleRewardIcon;

    [Header("長押し詳細パネル")]
    [SerializeField] private BoardRewardDetailPanel rewardDetailPanel;

    [Header("初回報酬ガイド")]
    [SerializeField] private BoardRewardTutorialPanel rewardTutorialPanel;
    [SerializeField] private bool showRewardUsageGuideOnFirstReward = true;
    [TextArea(2, 4)]
    [SerializeField] private string rewardGuideTitle = "回収品の扱い";
    [TextArea(4, 8)]
    [SerializeField] private string rewardGuideBody = "報酬は長押しで詳細を確認できます。\n攻撃アイテムは敵へドラッグして使用します。\n回復・弾薬アイテムはタップで使用します。";

    private PlayerCombatController playerCombatController;
    private BattleUIController battleUIController;
    private PanelBoardController panelBoardController;
    private BattleEventHub battleEventHub;

    private readonly HashSet<int> consumedBattles = new HashSet<int>();
    private readonly Dictionary<string, RewardOption> activeOptions = new Dictionary<string, RewardOption>();

    private bool rewardActive;
    private int activeBattleNumber;
    private bool rewardTutorialVisible;

    private const string RewardUsageGuideSeenPrefsKey = "board_reward_usage_guide_seen_v1";

    private class RewardOption
    {
        public string rewardId;
        public string shortLabel;
        public string displayName;
        public string detailText;
        public Sprite iconSprite;
        public Color iconTint;
        public Action apply;
        public string pickupText;
    }


    private void Awake()
    {
        AutoResolvePanels();
    }

    public void Initialize(
        PlayerCombatController playerCombatController,
        BattleUIController battleUIController,
        PanelBoardController panelBoardController,
        BattleEventHub battleEventHub)
    {
        AutoResolvePanels();

        this.playerCombatController = playerCombatController;
        this.battleUIController = battleUIController;
        this.panelBoardController = panelBoardController;
        this.battleEventHub = battleEventHub;

        if (this.panelBoardController != null)
        {
            this.panelBoardController.SetSpecialPanelClickHandler(TryHandleBoardRewardClick);
            this.panelBoardController.SetSpecialPanelLongPressHandlers(HandleRewardLongPressStart, HandleRewardLongPressEnd);
        }

        rewardDetailPanel?.HideImmediate();
        rewardTutorialPanel?.HideImmediate();
    }

    public IEnumerator TryPresentBoardRewardRoutine(int battleNumber)
    {
        if (rewardActive)
        {
            ExpireActiveRewardPanels();
        }

        if (battleNumber <= 0) yield break;
        if (consumedBattles.Contains(battleNumber)) yield break;
        if (playerCombatController == null || panelBoardController == null) yield break;

        List<RewardOption> options = BuildOptionsForBattle(battleNumber);
        if (options == null || options.Count == 0) yield break;

        List<BoardRewardPanelCell> cells = new List<BoardRewardPanelCell>();
        activeOptions.Clear();

        foreach (RewardOption option in options)
        {
            if (option == null || string.IsNullOrEmpty(option.rewardId)) continue;

            activeOptions[option.rewardId] = option;
            cells.Add(new BoardRewardPanelCell
            {
                rewardId = option.rewardId,
                shortLabel = option.shortLabel,
                detailText = option.detailText,
                iconSprite = option.iconSprite,
                iconTint = option.iconTint,
            });
        }

        if (cells.Count == 0) yield break;
        if (!panelBoardController.TryPlaceRewardPanels(cells))
        {
            activeOptions.Clear();
            yield break;
        }

        consumedBattles.Add(battleNumber);
        activeBattleNumber = battleNumber;
        rewardActive = true;

        if (ShouldShowRewardUsageGuide(battleNumber))
        {
            ShowRewardUsageGuide();
        }
        else
        {
            ShowPromptText(battleNumber);
        }
    }

    private List<RewardOption> BuildOptionsForBattle(int battleNumber)
    {
        switch (battleNumber)
        {
            case GuaranteedSwordBattleNumber:
                return BuildSingleRewardList(BuildSwordReward());
            case GuaranteedShotgunBattleNumber:
                return BuildSingleRewardList(BuildShotgunReward());
            case GuaranteedGreatSwordBattleNumber:
                return BuildSingleRewardList(BuildGreatSwordReward());
            case GuaranteedRifleBattleNumber:
                return BuildSingleRewardList(BuildRifleReward());
            default:
                return null;
        }
    }

    private static List<RewardOption> BuildSingleRewardList(RewardOption option)
    {
        if (option == null) return null;
        return new List<RewardOption> { option };
    }

    private RewardOption BuildSwordReward()
    {
        return new RewardOption
        {
            rewardId = "weapon_sword",
            shortLabel = "剣",
            displayName = "剣",
            detailText = "剣\n最大4リンク\n標準的な近接武器。盤面の触り心地を素直に強くする。",
            iconSprite = ResolveWeaponRewardIcon(WeaponRewardKind.Sword),
            iconTint = new Color(1f, 0.92f, 0.45f, 1f),
            pickupText = "剣を回収",
            apply = () =>
            {
                playerCombatController?.EquipSword();
                battleUIController?.RefreshInventoryUI();
                battleUIController?.RefreshGunUI();
            }
        };
    }

    private RewardOption BuildGreatSwordReward()
    {
        return new RewardOption
        {
            rewardId = "weapon_greatsword",
            shortLabel = "大",
            displayName = "大剣",
            detailText = "大剣\n最大5リンク\n4リンクを十分味わった後に解禁される、後半のご褒美武器。",
            iconSprite = ResolveWeaponRewardIcon(WeaponRewardKind.GreatSword),
            iconTint = new Color(1f, 0.72f, 0.28f, 1f),
            pickupText = "大剣を回収",
            apply = () =>
            {
                playerCombatController?.EquipGreatSword();
                battleUIController?.RefreshInventoryUI();
                battleUIController?.RefreshGunUI();
            }
        };
    }

    private RewardOption BuildShotgunReward()
    {
        GunData data = PlayerCombatController.CreatePrototypeGunData(GunType.Shotgun);
        return BuildGunRewardOption(data, "gun_shotgun", "SG", "ショットガン", "危険敵の迎撃や攻撃直前の敵を止めるための中盤主力銃。", new Color(1f, 0.78f, 0.48f, 1f));
    }

    private RewardOption BuildRifleReward()
    {
        GunData data = PlayerCombatController.CreatePrototypeGunData(GunType.Rifle);
        return BuildGunRewardOption(data, "gun_rifle", "RF", "ライフル", "終盤の重い敵・後衛・危険個体を一撃で処理するための高火力銃。", new Color(1f, 0.9f, 0.4f, 1f));
    }

    private RewardOption BuildGunRewardOption(GunData data, string rewardId, string shortLabel, string displayName, string roleText, Color tint)
    {
        if (data == null) return null;

        return new RewardOption
        {
            rewardId = rewardId,
            shortLabel = shortLabel,
            displayName = displayName,
            detailText = displayName + "\n" + roleText,
            iconSprite = ResolveGunRewardIcon(data.gunType),
            iconTint = tint,
            pickupText = displayName + "を回収",
            apply = () =>
            {
                playerCombatController?.EquipGun(data, false);
                battleUIController?.RefreshGunUI();
                battleUIController?.RefreshInventoryUI();
            }
        };
    }

    private bool TryHandleBoardRewardClick(int row, int col)
    {
        if (!rewardActive || panelBoardController == null)
        {
            return false;
        }

        if (rewardTutorialVisible)
        {
            return true;
        }

        BoardRewardPanelCell cell = panelBoardController.GetRewardPanelAt(row, col);
        if (cell == null)
        {
            return false;
        }

        if (!activeOptions.TryGetValue(cell.rewardId, out RewardOption option) || option == null)
        {
            ConsumeActiveRewardPanels();
            return true;
        }

        HideRewardDetailPanel();
        option.apply?.Invoke();

        Vector3 pos = panelBoardController.GetPanelWorldPosition(row, col) + Vector3.up * 0.35f;
        battleEventHub?.RaiseDamageTextRequested(option.pickupText, pos, new Color(1f, 0.92f, 0.5f, 1f));

        ConsumeActiveRewardPanels();
        return true;
    }

    private void HandleRewardLongPressStart(int row, int col)
    {
        if (!rewardActive || panelBoardController == null) return;
        if (rewardTutorialVisible) return;

        BoardRewardPanelCell cell = panelBoardController.GetRewardPanelAt(row, col);
        if (cell == null) return;

        if (!activeOptions.TryGetValue(cell.rewardId, out RewardOption option) || option == null) return;

        if (rewardDetailPanel != null)
        {
            rewardDetailPanel.Show(option.displayName, option.detailText, option.iconSprite, option.iconTint);
            return;
        }

        Vector3 pos = panelBoardController.GetPanelWorldPosition(row, col) + Vector3.up * 1.8f;
        battleEventHub?.RaiseDamageTextRequested(option.detailText, pos, new Color(0.9f, 0.95f, 1f, 1f));
    }

    private void HandleRewardLongPressEnd(int row, int col)
    {
        HideRewardDetailPanel();
    }

    private void ShowPromptText(int battleNumber)
    {
        string prompt;
        switch (battleNumber)
        {
            case GuaranteedSwordBattleNumber:
                prompt = "武器回収！\n剣を拾って4リンク解禁\nタップで取得 / 長押しで詳細";
                break;
            case GuaranteedShotgunBattleNumber:
                prompt = "武装回収！\nショットガン解禁\nタップで取得 / 長押しで詳細";
                break;
            case GuaranteedGreatSwordBattleNumber:
                prompt = "武器回収！\n大剣を拾って5リンク解禁\nタップで取得 / 長押しで詳細";
                break;
            case GuaranteedRifleBattleNumber:
                prompt = "武装回収！\nライフル解禁\nタップで取得 / 長押しで詳細";
                break;
            default:
                prompt = "回収！\nタップで取得 / 長押しで詳細";
                break;
        }

        Vector3 pos = Vector3.zero;
        if (panelBoardController != null)
        {
            pos = panelBoardController.GetPanelWorldPosition(2, 2) + Vector3.up * 2.6f;
        }

        battleEventHub?.RaiseDamageTextRequested(prompt, pos, promptTextColor);
    }


    private bool ShouldShowRewardUsageGuide(int battleNumber)
    {
        if (!showRewardUsageGuideOnFirstReward)
        {
            return false;
        }

        if (rewardTutorialPanel == null)
        {
            return false;
        }

        if (battleNumber != GuaranteedSwordBattleNumber)
        {
            return false;
        }

        return PlayerPrefs.GetInt(RewardUsageGuideSeenPrefsKey, 0) == 0;
    }

    private void ShowRewardUsageGuide()
    {
        if (rewardTutorialPanel == null)
        {
            ShowPromptText(activeBattleNumber);
            return;
        }

        rewardTutorialVisible = true;
        rewardTutorialPanel.Show(rewardGuideTitle, rewardGuideBody, OnConfirmRewardUsageGuide);
    }

    private void OnConfirmRewardUsageGuide()
    {
        PlayerPrefs.SetInt(RewardUsageGuideSeenPrefsKey, 1);
        PlayerPrefs.Save();

        HideRewardUsageGuide();
        ShowPromptText(activeBattleNumber);
    }

    private void HideRewardUsageGuide()
    {
        rewardTutorialVisible = false;
        rewardTutorialPanel?.Hide();
    }

    private void ConsumeActiveRewardPanels()
    {
        HideRewardDetailPanel();
        HideRewardUsageGuide();
        panelBoardController?.ClearRewardPanels(true);
        activeOptions.Clear();
        activeBattleNumber = 0;
        rewardActive = false;
    }

    private void ExpireActiveRewardPanels()
    {
        if (!rewardActive)
        {
            return;
        }

        Vector3 pos = Vector3.zero;
        if (panelBoardController != null)
        {
            pos = panelBoardController.GetPanelWorldPosition(2, 2) + Vector3.up * 2.2f;
        }

        battleEventHub?.RaiseDamageTextRequested(
            "未回収の報酬は消失",
            pos,
            new Color(1f, 0.72f, 0.32f, 1f));

        ConsumeActiveRewardPanels();
    }

    private void HideRewardDetailPanel()
    {
        rewardDetailPanel?.Hide();
    }

    private Sprite ResolveWeaponRewardIcon(WeaponRewardKind kind)
    {
        switch (kind)
        {
            case WeaponRewardKind.Sword:
                if (swordRewardIcon != null) return swordRewardIcon;
                return panelBoardController != null ? panelBoardController.GetDisplaySpriteForPanelType(PanelType.Sword) : null;

            case WeaponRewardKind.GreatSword:
                if (greatSwordRewardIcon != null) return greatSwordRewardIcon;
                if (swordRewardIcon != null) return swordRewardIcon;
                return panelBoardController != null ? panelBoardController.GetDisplaySpriteForPanelType(PanelType.Sword) : null;

            default:
                return null;
        }
    }

    private Sprite ResolveGunRewardIcon(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                if (pistolRewardIcon != null) return pistolRewardIcon;
                break;
            case GunType.MachineGun:
                if (machineGunRewardIcon != null) return machineGunRewardIcon;
                break;
            case GunType.Shotgun:
                if (shotgunRewardIcon != null) return shotgunRewardIcon;
                break;
            case GunType.Rifle:
                if (rifleRewardIcon != null) return rifleRewardIcon;
                break;
        }

        return panelBoardController != null ? panelBoardController.GetDisplaySpriteForPanelType(PanelType.Ammo) : null;
    }


    private void AutoResolvePanels()
    {
        if (rewardDetailPanel == null)
        {
            rewardDetailPanel = FindSceneObjectIncludingInactive<BoardRewardDetailPanel>();
        }

        if (rewardTutorialPanel == null)
        {
            rewardTutorialPanel = FindSceneObjectIncludingInactive<BoardRewardTutorialPanel>();
        }
    }

    private static T FindSceneObjectIncludingInactive<T>() where T : Component
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        if (objects == null || objects.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            T candidate = objects[i];
            if (candidate == null)
            {
                continue;
            }

            GameObject go = candidate.gameObject;
            if (go == null)
            {
                continue;
            }

            if (!go.scene.IsValid())
            {
                continue;
            }

            if ((go.hideFlags & HideFlags.NotEditable) != 0)
            {
                continue;
            }

            if ((go.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private enum WeaponRewardKind
    {
        Sword,
        GreatSword,
    }
}
