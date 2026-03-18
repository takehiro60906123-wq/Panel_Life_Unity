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

    [Header("Turn Hint UI")]
    [SerializeField] private Image turnIndicatorImage;
    [SerializeField] private bool autoFindTurnIndicatorIfMissing = true;

    private Color cachedTurnIndicatorBaseColor = Color.white;
    private bool hasCachedTurnIndicatorBaseColor;
    private Color cachedTurnTextBaseColor = Color.white;
    private bool hasCachedTurnTextBaseColor;

    private EnemyTweenPresenter tweenPresenter;
    private PlayerAnimationPresenter playerAnimationPresenter;
    private BattleUnit battleUnit;
    private PlayerCombatController cachedPlayerCombatController;

    private GunType lastHintGunType = GunType.None;
    private EnemyType lastHintEnemyType = EnemyType.Normal;
    private bool lastHintDanger;
    private bool hintInitialized;

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

        if (battleUnit == null)
        {
            battleUnit = GetComponent<BattleUnit>();
        }

        tweenPresenter?.EnsureSetup();
        CacheTurnIndicatorReferences();
        RefreshTurnHintFromCurrentState(force: true);
    }

    private void Awake()
    {
        if (battleUnit == null)
        {
            battleUnit = GetComponent<BattleUnit>();
        }

        CacheTurnIndicatorReferences();
    }

    private void Update()
    {
        RefreshTurnHintFromCurrentState();
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

        RefreshTurnHintFromCurrentState(force: true);
    }

    private void RefreshTurnHintFromCurrentState(bool force = false)
    {
        if (battleUnit == null)
        {
            battleUnit = GetComponent<BattleUnit>();
        }

        PlayerCombatController playerCombat = GetPlayerCombatController();
        GunType gunType = GunType.None;
        if (playerCombat != null)
        {
            GunData gun = playerCombat.GetGunData();
            gunType = gun != null ? gun.gunType : GunType.None;
        }

        EnemyType enemyType = battleUnit != null ? battleUnit.enemyType : EnemyType.Normal;
        bool isDanger = battleUnit != null && battleUnit.IsDangerEnemy();

        if (!force && hintInitialized && gunType == lastHintGunType && enemyType == lastHintEnemyType && isDanger == lastHintDanger)
        {
            return;
        }

        lastHintGunType = gunType;
        lastHintEnemyType = enemyType;
        lastHintDanger = isDanger;
        hintInitialized = true;

        ApplyTurnIndicatorTint(gunType, enemyType, isDanger);
    }

    private PlayerCombatController GetPlayerCombatController()
    {
        if (cachedPlayerCombatController == null)
        {
#if UNITY_2023_1_OR_NEWER
            cachedPlayerCombatController = FindFirstObjectByType<PlayerCombatController>();
#else
            cachedPlayerCombatController = FindObjectOfType<PlayerCombatController>();
#endif
        }

        return cachedPlayerCombatController;
    }

    private void CacheTurnIndicatorReferences()
    {
        if (turnText != null && !hasCachedTurnTextBaseColor)
        {
            cachedTurnTextBaseColor = turnText.color;
            hasCachedTurnTextBaseColor = true;
        }

        if (turnIndicatorImage == null && autoFindTurnIndicatorIfMissing && turnText != null)
        {
            turnIndicatorImage = turnText.GetComponentInParent<Image>();
        }

        if (turnIndicatorImage != null && !hasCachedTurnIndicatorBaseColor)
        {
            cachedTurnIndicatorBaseColor = turnIndicatorImage.color;
            hasCachedTurnIndicatorBaseColor = true;
        }
    }

    private void ApplyTurnIndicatorTint(GunType gunType, EnemyType enemyType, bool isDangerEnemy)
    {
        CacheTurnIndicatorReferences();

        Color baseIconColor = hasCachedTurnIndicatorBaseColor ? cachedTurnIndicatorBaseColor : Color.white;
        Color baseTextColor = hasCachedTurnTextBaseColor ? cachedTurnTextBaseColor : Color.white;
        Color targetColor = ResolveTurnHintColor(gunType, enemyType, isDangerEnemy, baseIconColor);

        if (turnIndicatorImage != null)
        {
            turnIndicatorImage.color = targetColor;
        }

        if (turnText != null)
        {
            turnText.color = Color.Lerp(baseTextColor, targetColor, 0.85f);
        }
    }

    private Color ResolveTurnHintColor(GunType gunType, EnemyType enemyType, bool isDangerEnemy, Color baseColor)
    {
        // 余計な色を挟まず、「有効なら黄色 / それ以外は元色」に固定する。
        // danger 用の赤をここで混ぜると、同じ敵でも残りターンで色が変わり、
        // 有効判定がぶれて見える原因になる。
        if (IsGunEffectiveAgainstHintEnemy(gunType, enemyType))
        {
            return new Color(1f, 0.92f, 0.35f, baseColor.a);
        }

        return baseColor;
    }

    private bool IsGunEffectiveAgainstHintEnemy(GunType gunType, EnemyType enemyType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return enemyType == EnemyType.Ranged || enemyType == EnemyType.Rushing;

            case GunType.Rifle:
                return enemyType == EnemyType.Floating || enemyType == EnemyType.Ranged || enemyType == EnemyType.Armored;

            case GunType.Shotgun:
                return enemyType == EnemyType.Armored || enemyType == EnemyType.Rushing;

            case GunType.MachineGun:
                return enemyType == EnemyType.Rushing;

            default:
                return false;
        }
    }

    public void RefreshGunAdvantageHint()
    {
        RefreshTurnHintFromCurrentState(force: true);
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

    public void PlayHeavyDamaged(bool dead)
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
                tweenPresenter.PlayHeavyHitTween();
            }

            return;
        }

        PlayDamaged(dead);
    }

    // =============================================================
    // ʏUij
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
    // Uo
    // =============================================================

    /// <summary>
    /// HeavyHit ߃^[F̂kăIWɌ
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
    /// HeavyHit ˃^[Fʏ傫ːiĐԃIWɌ
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
    /// PanelCorrupt XLFɖĔg
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
    /// SelfBuff 񕜁F΂Ɍď
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
    // \bh
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