using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    private Slider hpSlider;
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI turnText;
    private Animator animator;

    public void BindLegacyReferences(
        Slider slider,
        TextMeshProUGUI hp,
        TextMeshProUGUI level,
        TextMeshProUGUI turn,
        Animator anim)
    {
        hpSlider = slider;
        hpText = hp;
        levelText = level;
        turnText = turn;
        animator = anim;
    }

    public void RefreshHP(int currentHP, int maxHP)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }
    }

    public void RefreshLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv {level}";
        }
    }

    public void RefreshCooldown(int cooldown)
    {
        if (turnText == null) return;

        if (cooldown > 0)
        {
            turnText.text = $"‚ ‚Æ {cooldown}";
        }
        else
        {
            turnText.text = "ATTACK!";
        }
    }

    public void PlayDamaged(bool dead)
    {
        if (animator == null) return;

        if (dead)
        {
            animator.Play("DEATH", 0, 0f);
        }
        else
        {
            animator.Play("DAMAGED", 0, 0f);
        }
    }

    public void PlayHeal()
    {
        if (animator != null)
        {
            animator.SetTrigger("6_Other");
        }
    }

    public void PlayIdle()
    {
        if (animator != null)
        {
            animator.Play("IDLE", 0, 0f);
        }
    }

    public void SetUIActive(bool isActive)
    {
        if (hpSlider != null) hpSlider.gameObject.SetActive(isActive);
        if (hpText != null) hpText.gameObject.SetActive(isActive);
        if (levelText != null) levelText.gameObject.SetActive(isActive);
        if (turnText != null) turnText.gameObject.SetActive(isActive);
    }
}