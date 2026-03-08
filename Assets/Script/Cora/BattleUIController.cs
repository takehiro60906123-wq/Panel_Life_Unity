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

        if (pistolButton != null)
        {
            GunData gun = playerCombatController.GetGunData();

            bool canUse = false;
            if (gun != null)
            {
                if (gun.gunType == GunType.MachineGun)
                {
                    canUse = playerCombatController.CanUseMachineGun();
                }
                else
                {
                    canUse = playerCombatController.CanUseGun();
                }
            }

            pistolButton.interactable = canUse;
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