using UnityEngine;
using TMPro;

public class BattleUIController : MonoBehaviour
{
    [Header("グローバルUI")]
    public TextMeshProUGUI coinText;

    [Header("探索UI")]
    public TextMeshProUGUI encounterLabelText;
    public TextMeshProUGUI encounterStepText;

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