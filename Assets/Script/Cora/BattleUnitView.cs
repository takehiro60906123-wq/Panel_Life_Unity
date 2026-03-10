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
    private EnemyTweenPresenter tweenPresenter;

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

        if (tweenPresenter == null)
        {
            tweenPresenter = GetComponent<EnemyTweenPresenter>();
        }

        tweenPresenter?.EnsureSetup();
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
        if (tweenPresenter != null)
        {
            if (dead)
            {
                tweenPresenter.PlayDeathTween();
            }
            else
            {
                tweenPresenter.PlayHitTween();
            }

            return;
        }

        if (dead)
        {
            TryPlayState("DEATH");
        }
        else
        {
            TryPlayState("DAMAGED");
        }
    }

    public void PlayAttack()
    {
        if (tweenPresenter != null)
        {
            tweenPresenter.PlayAttackTween();
            return;
        }

        TryPlayState("ATTACK");
    }

    public void PlayHeal()
    {
        if (animator != null && HasTriggerParameter("6_Other"))
        {
            animator.SetTrigger("6_Other");
        }
    }

    public void PlayIdle()
    {
        tweenPresenter?.PlayIdleReset();
        TryPlayState("IDLE");
    }

    public void SetUIActive(bool isActive)
    {
        if (hpSlider != null) hpSlider.gameObject.SetActive(isActive);
        if (hpText != null) hpText.gameObject.SetActive(isActive);
        if (levelText != null) levelText.gameObject.SetActive(isActive);
        if (turnText != null) turnText.gameObject.SetActive(isActive);
    }

    private bool TryPlayState(string stateName)
    {
        if (animator == null) return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return false;
        }

        animator.Play(stateHash, 0, 0f);
        return true;
    }

    private bool HasTriggerParameter(string paramName)
    {
        if (animator == null) return false;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter p = animator.parameters[i];
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
            {
                return true;
            }
        }

        return false;
    }
}