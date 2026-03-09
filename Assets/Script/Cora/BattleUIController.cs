using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIController : MonoBehaviour
{
    [Header("グローバルUI")]
    public TextMeshProUGUI coinText;

    [Header("探索UI")]
    public TextMeshProUGUI encounterLabelText;
    public TextMeshProUGUI encounterStepText;

    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private Button pistolButton;
    [SerializeField] private TMP_Text gunGaugeText;
    [SerializeField] private PanelBattleManager panelBattleManager;

    [SerializeField] private UnityEngine.UI.Image[] ammoImages;
    [SerializeField] private TMP_Text ammoCountText;

    private void Start()
    {
        if (pistolButton != null)
        {
            pistolButton.onClick.RemoveAllListeners();
            pistolButton.onClick.AddListener(OnClickPistol);
        }

        RefreshGunUI();
    }

    public void RefreshGunUI()
    {
        if (playerCombatController == null) return;

        int current = playerCombatController.GetGunGauge();
        int max = playerCombatController.GetGunGaugeMax();

        if (gunGaugeText != null)
        {
            gunGaugeText.text = $"GUN {current}/{max}";
        }

        if (ammoCountText != null)
        {
            ammoCountText.text = $"{current}/{max}";
        }

        RefreshAmmoIcons(current, max);

        if (pistolButton != null)
        {
            GunData gun = playerCombatController.GetGunData();

            bool canUse = false;
            if (gun != null)
            {
                switch (gun.gunType)
                {
                    case GunType.MachineGun:
                        canUse = playerCombatController.CanUseMachineGun();
                        break;

                    case GunType.Pistol:
                    case GunType.Shotgun:
                    case GunType.Rifle:
                        canUse = playerCombatController.CanUseGun();
                        break;
                }
            }

            pistolButton.interactable = canUse;
        }
    }

    private void RefreshAmmoIcons(int current, int max)
    {
        if (ammoImages == null || ammoImages.Length == 0) return;

        int displayCount = Mathf.Min(ammoImages.Length, max);

        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] == null) continue;

            bool isActiveAmmo = i < current && i < displayCount;

            ammoImages[i].color = isActiveAmmo
                ? Color.white
                : new Color(1f, 1f, 1f, 0.2f);
        }
    }

    private void OnClickPistol()
    {
        Debug.Log("OnClickPistol: ボタン押下");

        if (panelBattleManager == null)
        {
            Debug.Log("OnClickPistol: panelBattleManager が NULL");
            return;
        }

        if (playerCombatController == null)
        {
            Debug.Log("OnClickPistol: playerCombatController が NULL");
            return;
        }

        GunData gun = playerCombatController.GetGunData();
        if (gun == null)
        {
            Debug.Log("OnClickPistol: gun が NULL");
            return;
        }

        switch (gun.gunType)
        {
            case GunType.Pistol:
                panelBattleManager.FirePistol();
                break;

            case GunType.MachineGun:
                panelBattleManager.FireMachineGun();
                break;

            case GunType.Shotgun:
                panelBattleManager.FireShotgun();
                break;

            case GunType.Rifle:
                panelBattleManager.FireRifle();
                break;

            default:
                Debug.Log("OnClickPistol: 未対応の銃タイプ");
                break;
        }
    }

    public void SetCoinText(int coins)
    {
        if (coinText != null)
        {
            coinText.text = coins.ToString();
        }
    }

    public void SetEncounterInfo(EncounterType encounterType, int remainingSteps)
    {
        if (encounterLabelText != null)
        {
            switch (encounterType)
            {
                case EncounterType.Empty:
                    encounterLabelText.text = "平和な部屋";
                    break;

                case EncounterType.Treasure:
                    encounterLabelText.text = "宝箱の部屋";
                    break;

                default:
                    encounterLabelText.text = "";
                    break;
            }
        }

        if (encounterStepText != null)
        {
            if (encounterType == EncounterType.Empty || encounterType == EncounterType.Treasure)
            {
                encounterStepText.text = $"あと {remainingSteps} ターン";
            }
            else
            {
                encounterStepText.text = "";
            }
        }
    }
}