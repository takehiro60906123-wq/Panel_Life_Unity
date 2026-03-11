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
    private PlayerAnimationPresenter playerAnimationPresenter;

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

        if (playerAnimationPresenter == null)
        {
            playerAnimationPresenter = GetComponent<PlayerAnimationPresenter>();
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
            turnText.text = cooldown.ToString();
        }
        else
        {
            turnText.text = "!";
        }
    }

    public void PlayDamaged(bool dead)
    {
        if (playerAnimationPresenter != null)
        {
            if (dead)
            {
                playerAnimationPresenter.PlaySpin();
            }
            else
            {
                playerAnimationPresenter.PlayHurt();
            }

            return;
        }

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

    // =============================================================
    // 通常攻撃（既存）
    // =============================================================

    public void PlayAttack()
    {
        if (playerAnimationPresenter != null)
        {
            playerAnimationPresenter.PlayRunShoot();
            return;
        }

        if (tweenPresenter != null)
        {
            tweenPresenter.PlayAttackTween();
            return;
        }

        TryPlayState("ATTACK");
    }

    // =============================================================
    // 特殊攻撃演出
    // =============================================================

    /// <summary>
    /// HeavyHit 溜めターン：体が震えてオレンジに光る
    /// </summary>
    public void PlayCharge()
    {
        if (tweenPresenter != null)
        {
            tweenPresenter.PlayChargeTween();
            return;
        }

        TryPlayState("ATTACK");
    }

    /// <summary>
    /// HeavyHit 発射ターン：通常より大きく突進して赤オレンジに光る
    /// </summary>
    public void PlayHeavyAttack()
    {
        if (tweenPresenter != null)
        {
            tweenPresenter.PlayHeavyAttackTween();
            return;
        }

        TryPlayState("ATTACK");
    }

    /// <summary>
    /// PanelCorrupt スキル発動：紫に脈動して波動を放つ
    /// </summary>
    public void PlayCorruptSkill()
    {
        if (tweenPresenter != null)
        {
            tweenPresenter.PlayCorruptSkillTween();
            return;
        }

        TryPlayState("ATTACK");
    }

    /// <summary>
    /// SelfBuff 回復：緑に光って少し浮く
    /// </summary>
    public void PlayEnemyHeal()
    {
        if (tweenPresenter != null)
        {
            tweenPresenter.PlayHealTween();
            return;
        }

        PlayHeal();
    }

    // =============================================================
    // 既存メソッド
    // =============================================================

    public void PlayHeal()
    {
        if (playerAnimationPresenter != null)
        {
            playerAnimationPresenter.PlayIdle();
            return;
        }

        if (animator != null && HasTriggerParameter("6_Other"))
        {
            animator.SetTrigger("6_Other");
        }
    }

    public void PlayIdle()
    {
        if (playerAnimationPresenter != null)
        {
            playerAnimationPresenter.PlayIdle();
            return;
        }

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