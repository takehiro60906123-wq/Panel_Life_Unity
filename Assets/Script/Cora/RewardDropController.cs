using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RewardDropController : MonoBehaviour
{
    [Header("長押し詳細")]
    // Unity Inspector の Serialized List バインド例外を避けるため、
    // 固定報酬タイミングはコード定数で持つ。
    private static readonly int GuaranteedWeaponBattleNumber = 3;
    private static readonly int GuaranteedGunBattleNumber1 = 10;
    private static readonly int GuaranteedGunBattleNumber2 = 20;

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

    private PlayerCombatController playerCombatController;
    private BattleUIController battleUIController;
    private PanelBoardController panelBoardController;
    private BattleEventHub battleEventHub;

    private readonly HashSet<int> consumedBattles = new HashSet<int>();
    private readonly Dictionary<string, RewardOption> activeOptions = new Dictionary<string, RewardOption>();

    private bool rewardActive;
    private bool waitingSelection;
    private int activeBattleNumber;

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

    public void Initialize(
        PlayerCombatController playerCombatController,
        BattleUIController battleUIController,
        PanelBoardController panelBoardController,
        BattleEventHub battleEventHub)
    {
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
    }

    public IEnumerator TryPresentBoardRewardRoutine(int battleNumber)
    {
        // 前回の未取得報酬は、次の敵撃破時点で消す
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

        ShowPromptText(battleNumber, cells.Count);
    }

    private List<RewardOption> BuildOptionsForBattle(int battleNumber)
    {
        if (battleNumber == GuaranteedWeaponBattleNumber)
        {
            return BuildWeaponOptions();
        }

        if (battleNumber == GuaranteedGunBattleNumber1 || battleNumber == GuaranteedGunBattleNumber2)
        {
            return BuildGunOptions();
        }

        return null;
    }

    private List<RewardOption> BuildWeaponOptions()
    {
        List<RewardOption> list = new List<RewardOption>();
        Sprite swordSprite = ResolveWeaponRewardIcon(WeaponRewardKind.Sword);
        Sprite greatSwordSprite = ResolveWeaponRewardIcon(WeaponRewardKind.GreatSword);

        list.Add(new RewardOption
        {
            rewardId = "weapon_sword",
            shortLabel = "剣",
            displayName = "剣",
            detailText = "剣\n最大4リンク\n標準的な近接武器。盤面の触り心地を素直に強くする。",
            iconSprite = swordSprite,
            iconTint = new Color(1f, 0.92f, 0.45f, 1f),
            pickupText = "剣を回収",
            apply = () =>
            {
                playerCombatController?.EquipSword();
                battleUIController?.RefreshInventoryUI();
                battleUIController?.RefreshGunUI();
            }
        });

        list.Add(new RewardOption
        {
            rewardId = "weapon_greatsword",
            shortLabel = "大",
            displayName = "大剣",
            detailText = "大剣\n最大5リンク\n今は純粋強化として運用。将来は重さ差別化を乗せやすい枠。",
            iconSprite = greatSwordSprite,
            iconTint = new Color(1f, 0.72f, 0.28f, 1f),
            pickupText = "大剣を回収",
            apply = () =>
            {
                playerCombatController?.EquipGreatSword();
                battleUIController?.RefreshInventoryUI();
                battleUIController?.RefreshGunUI();
            }
        });

        return list;
    }

    private List<RewardOption> BuildGunOptions()
    {
        List<GunType> pool = new List<GunType>
        {
            GunType.Pistol,
            GunType.MachineGun,
            GunType.Shotgun,
            GunType.Rifle,
        };

        GunData current = playerCombatController != null ? playerCombatController.GetGunData() : null;
        if (current != null)
        {
            pool.Remove(current.gunType);
        }

        Shuffle(pool);

        List<RewardOption> list = new List<RewardOption>();

        int count = Mathf.Min(2, pool.Count);
        for (int i = 0; i < count; i++)
        {
            GunData data = CreateGunData(pool[i]);
            if (data == null) continue;

            list.Add(new RewardOption
            {
                rewardId = "gun_" + data.gunType.ToString().ToLowerInvariant(),
                shortLabel = BuildGunShortLabel(data.gunType),
                displayName = data.gunName,
                detailText = BuildGunDetail(data),
                iconSprite = ResolveGunRewardIcon(data.gunType),
                iconTint = ResolveGunTint(data.gunType),
                pickupText = data.gunName + "を回収",
                apply = () =>
                {
                    if (playerCombatController == null || playerCombatController.loadout == null) return;
                    playerCombatController.loadout.gun = CloneGunData(data);
                    battleUIController?.RefreshGunUI();
                    battleUIController?.RefreshInventoryUI();
                }
            });
        }

        return list;
    }

    private bool TryHandleBoardRewardClick(int row, int col)
    {
        if (!rewardActive || panelBoardController == null)
        {
            return false;
        }

        BoardRewardPanelCell cell = panelBoardController.GetRewardPanelAt(row, col);
        if (cell == null)
        {
            // 非報酬セルは通常処理へ流す
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

    private void FinishRewardSelection()
    {
        ConsumeActiveRewardPanels();
    }

    private void ShowPromptText(int battleNumber, int optionCount)
    {
        string prefix = (battleNumber == GuaranteedWeaponBattleNumber)
            ? "武器回収"
            : "武装回収";

        Vector3 pos = Vector3.zero;
        if (panelBoardController != null)
        {
            pos = panelBoardController.GetPanelWorldPosition(2, 2) + Vector3.up * 2.6f;
        }

        battleEventHub?.RaiseDamageTextRequested(
            $"{prefix}!\n次の1戦の間だけ取得可能\nタップで取得 / 長押しで詳細",
            pos,
            promptTextColor);
    }

    private void ConsumeActiveRewardPanels()
    {
        HideRewardDetailPanel();
        panelBoardController?.ClearRewardPanels(true);
        activeOptions.Clear();
        activeBattleNumber = 0;
        rewardActive = false;
        waitingSelection = false;
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

    private enum WeaponRewardKind
    {
        Sword,
        GreatSword,
    }

    private static void Shuffle<T>(IList<T> list)
    {
        if (list == null) return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private static string BuildGunShortLabel(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol: return "P";
            case GunType.MachineGun: return "SMG";
            case GunType.Shotgun: return "SG";
            case GunType.Rifle: return "RF";
            default: return gunType.ToString();
        }
    }

    private static Color ResolveGunTint(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol: return new Color(0.92f, 0.92f, 0.92f, 1f);
            case GunType.MachineGun: return new Color(0.72f, 0.9f, 1f, 1f);
            case GunType.Shotgun: return new Color(1f, 0.78f, 0.48f, 1f);
            case GunType.Rifle: return new Color(1f, 0.9f, 0.4f, 1f);
            default: return Color.white;
        }
    }

    private static string BuildGunDetail(GunData data)
    {
        if (data == null) return string.Empty;

        switch (data.gunType)
        {
            case GunType.Pistol:
                return "ピストル\n低コスト2連射。危険敵への保険。";
            case GunType.MachineGun:
                return "サブマ\n手数で押す全消費連射。";
            case GunType.Shotgun:
                return "ショットガン\n近距離制圧。複数ヒットの高圧力。";
            case GunType.Rifle:
                return "ライフル\n単発高火力。重い敵と後衛処理向き。";
            default:
                return data.gunName;
        }
    }

    private static GunData CloneGunData(GunData source)
    {
        if (source == null) return null;

        return new GunData
        {
            gunType = source.gunType,
            gunName = source.gunName,
            gaugeCost = source.gaugeCost,
            shotCount = source.shotCount,
            damagePerShot = source.damagePerShot,
            scalingRate = source.scalingRate,
            useAllGauge = source.useAllGauge,
            minGaugeToFire = source.minGaugeToFire,
            shotInterval = source.shotInterval,
            finishDelay = source.finishDelay,
        };
    }

    private static GunData CreateGunData(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return new GunData
                {
                    gunType = GunType.Pistol,
                    gunName = "ピストル",
                    gaugeCost = 2,
                    shotCount = 2,
                    damagePerShot = 2,
                    scalingRate = 0.15f,
                    useAllGauge = false,
                    minGaugeToFire = 2,
                    shotInterval = 0.08f,
                    finishDelay = 0.22f,
                };
            case GunType.MachineGun:
                return new GunData
                {
                    gunType = GunType.MachineGun,
                    gunName = "サブマ",
                    gaugeCost = 0,
                    shotCount = 1,
                    damagePerShot = 1,
                    scalingRate = 0.1f,
                    useAllGauge = true,
                    minGaugeToFire = 3,
                    shotInterval = 0.04f,
                    finishDelay = 0.28f,
                };
            case GunType.Shotgun:
                return new GunData
                {
                    gunType = GunType.Shotgun,
                    gunName = "ショットガン",
                    gaugeCost = 5,
                    shotCount = 3,
                    damagePerShot = 1,
                    scalingRate = 0.2f,
                    useAllGauge = false,
                    minGaugeToFire = 5,
                    shotInterval = 0.05f,
                    finishDelay = 0.26f,
                };
            case GunType.Rifle:
                return new GunData
                {
                    gunType = GunType.Rifle,
                    gunName = "ライフル",
                    gaugeCost = 4,
                    shotCount = 1,
                    damagePerShot = 5,
                    scalingRate = 0.5f,
                    useAllGauge = false,
                    minGaugeToFire = 4,
                    shotInterval = 0.08f,
                    finishDelay = 0.24f,
                };
            default:
                return null;
        }
    }
}
