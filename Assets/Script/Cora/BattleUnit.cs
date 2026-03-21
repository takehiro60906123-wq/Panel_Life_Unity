using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerProgression))]
[RequireComponent(typeof(EnemyTurnState))]
[RequireComponent(typeof(BattleUnitView))]
public class BattleUnit : MonoBehaviour
{
    [Header("ステータス")]
    public int maxHP = 15;
    [SerializeField] private int currentHP;

    [Header("外殻")]
    [Tooltip("外殻の最大HP。0なら外殻なし")]
    public int maxShellHp = 0;
    [SerializeField] private int currentShellHp;

    [Header("報酬（敵専用）")]
    public int expYield = 2;
    public int coinYield = 3;

    [Header("敵戦闘パラメータ")]
    public int attackPower = 1;
    public EnemyType enemyType = EnemyType.Normal;
    public EnemyAttackPattern attackPattern = EnemyAttackPattern.Normal;
    [HideInInspector] public bool isChargingHeavyHit;

    [Header("モンスターパネル強化")]
    [Tooltip("モンスターパネルで何段階強化されたか")]
    public int enemyLevel = 0;

    [Header("レベルシステム（互換用）")]
    public int level = 1;
    public int currentExp = 0;
    public TextMeshProUGUI levelText;

    [Header("UI連携")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Slider shellSlider;
    public TextMeshProUGUI shellText;

    [Header("アニメーター")]
    public Animator animator;

    [Header("行動ターン（敵専用 / 互換用）")]
    public int attackInterval = 1;
    [HideInInspector] public int currentCooldown;
    public TextMeshProUGUI turnText;

    private PlayerProgression progression;
    private EnemyTurnState turnState;
    private BattleUnitView view;

    private StatusEffectHolder statusEffects;
    private BattleStatusIconPresenter statusIconPresenter;

    public StatusEffectHolder StatusEffects => statusEffects;
    public int CurrentHP => currentHP;
    public int CurrentShellHp => currentShellHp;
    public int MaxShellHp => maxShellHp;
    public bool HasActiveShell => currentShellHp > 0;

    public bool IsReadyToAttack()
    {
        return currentCooldown <= 0;
    }

    public bool IsDangerEnemy()
    {
        return currentCooldown <= 1;
    }

    private void Awake()
    {
        if (currentHP <= 0)
        {
            currentHP = maxHP;
        }

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        currentShellHp = Mathf.Clamp(currentShellHp <= 0 ? maxShellHp : currentShellHp, 0, Mathf.Max(0, maxShellHp));

        progression = GetComponent<PlayerProgression>();
        turnState = GetComponent<EnemyTurnState>();
        view = GetComponent<BattleUnitView>();

        statusEffects = GetComponent<StatusEffectHolder>();
        if (statusEffects == null)
        {
            statusEffects = gameObject.AddComponent<StatusEffectHolder>();
            Debug.Log($"[BattleUnit] {gameObject.name}: StatusEffectHolder を自動追加しました");
        }

        statusIconPresenter = GetComponent<BattleStatusIconPresenter>();
        if (statusIconPresenter == null)
        {
            statusIconPresenter = gameObject.AddComponent<BattleStatusIconPresenter>();
        }

        progression.Initialize(level, currentExp);
        turnState.Configure(attackInterval, currentCooldown);
        view.BindLegacyReferences(hpSlider, hpText, levelText, turnText, shellSlider, shellText, animator);

        statusIconPresenter.Initialize(this, statusEffects);
        statusIconPresenter.SetVisible(hpSlider == null || hpSlider.gameObject.activeSelf);

        SyncFromComponents();
        RefreshAll();
    }

    public void InitializeTurn()
    {
        turnState.SetAttackInterval(attackInterval);
        turnState.InitializeTurn();
        SyncFromComponents();
        UpdateTurnUI();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(int damage, bool useHeavyReaction)
    {
        if (damage <= 0) return;

        currentHP = Mathf.Max(0, currentHP - damage);

        if (view != null)
        {
            if (useHeavyReaction)
            {
                view.PlayHeavyDamaged(currentHP <= 0);
            }
            else
            {
                view.PlayDamaged(currentHP <= 0);
            }
        }

        RefreshAll();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        view.PlayHeal();
        RefreshAll();
    }

    public void UpdateUI()
    {
        view.RefreshHP(currentHP, maxHP, currentShellHp, maxShellHp);
        view.RefreshLevel(level);
    }

    public void UpdateTurnUI()
    {
        currentCooldown = turnState.CurrentCooldown;
        view.RefreshCooldown(currentCooldown);
    }

    public bool IsDead() => currentHP <= 0;

    public void Respawn()
    {
        currentHP = maxHP;
        currentShellHp = maxShellHp;
        view.PlayIdle();
        RefreshAll();
    }

    public void SetUIActive(bool isActive)
    {
        view.SetUIActive(isActive);
        statusIconPresenter?.SetVisible(isActive);
    }

    public void PlayAttackAnimation()
    {
        if (view != null)
        {
            view.PlayAttack();
            return;
        }

        if (animator != null)
        {
            animator.Play("ATTACK", 0, 0f);
        }
    }

    public void PlayChargeAnimation()
    {
        if (view != null) { view.PlayCharge(); return; }
        PlayAttackAnimation();
    }

    public void PlayHeavyAttackAnimation()
    {
        if (view != null) { view.PlayHeavyAttack(); return; }
        PlayAttackAnimation();
    }

    public void PlayCorruptSkillAnimation()
    {
        if (view != null) { view.PlayCorruptSkill(); return; }
        PlayAttackAnimation();
    }

    public void PlayEnemyHealAnimation()
    {
        if (view != null) { view.PlayEnemyHeal(); return; }
    }

    public void SetMoveAnimation(bool isMoving)
    {
        if (animator == null) return;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter p = animator.parameters[i];
            if (p.type == AnimatorControllerParameterType.Bool && p.name == "isMoving")
            {
                animator.SetBool("isMoving", isMoving);
                return;
            }
        }
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public bool AddExp(int amount)
    {
        bool leveledUp = progression.AddExp(amount, this);
        SyncFromComponents();
        RefreshAll();
        return leveledUp;
    }

    public void IncreaseMaxHP(int amount, bool fullRecover)
    {
        if (amount <= 0) return;

        maxHP += amount;

        if (fullRecover)
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP = Mathf.Min(currentHP, maxHP);
        }

        RefreshAll();
    }

    public void TickCooldown()
    {
        turnState.TickDown();
        SyncFromComponents();
        UpdateTurnUI();
    }

    public void ResetCooldown()
    {
        turnState.ResetCooldown();
        SyncFromComponents();
        UpdateTurnUI();
    }

    public void DelayCooldown(int amount)
    {
        turnState.Delay(amount);
        SyncFromComponents();
        UpdateTurnUI();
    }

    public void ApplyEncounterScaling(float hpMultiplier, int attackBonus, int rewardBonus)
    {
        hpMultiplier = Mathf.Max(1f, hpMultiplier);
        maxHP = Mathf.Max(1, Mathf.RoundToInt(maxHP * hpMultiplier));
        currentHP = Mathf.Max(1, Mathf.RoundToInt(currentHP * hpMultiplier));
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (maxShellHp > 0)
        {
            maxShellHp = Mathf.Max(1, Mathf.RoundToInt(maxShellHp * hpMultiplier));
            currentShellHp = Mathf.Clamp(Mathf.RoundToInt(currentShellHp * hpMultiplier), 0, maxShellHp);
        }

        attackPower = Mathf.Max(0, attackPower + attackBonus);
        expYield = Mathf.Max(0, expYield + rewardBonus);
        coinYield = Mathf.Max(0, coinYield + rewardBonus);

        RefreshAll();
    }

    public struct EnemyLevelUpResult
    {
        public int levelsGained;
        public int hpGained;
        public int healAmount;
        public int expBonusGained;
        public int newEnemyLevel;
    }

    public EnemyLevelUpResult EnemyLevelUp(int chainCount, int hpPerLevel = 2, int healPerLevel = 1, int expPerLevel = 1)
    {
        EnemyLevelUpResult result = new EnemyLevelUpResult();
        if (chainCount <= 0) return result;

        result.levelsGained = chainCount;
        enemyLevel += chainCount;

        int hpGain = chainCount * hpPerLevel;
        maxHP += hpGain;
        result.hpGained = hpGain;

        int heal = chainCount * healPerLevel;
        int hpBefore = currentHP;
        currentHP = Mathf.Min(currentHP + heal, maxHP);
        result.healAmount = currentHP - hpBefore;

        int expBonus = chainCount * expPerLevel;
        expYield += expBonus;
        result.expBonusGained = expBonus;

        result.newEnemyLevel = enemyLevel;

        RefreshAll();
        return result;
    }

    public void SetMaxShell(int value, bool refillCurrent)
    {
        maxShellHp = Mathf.Max(0, value);

        if (refillCurrent)
        {
            currentShellHp = maxShellHp;
        }
        else
        {
            currentShellHp = Mathf.Clamp(currentShellHp, 0, maxShellHp);
        }

        RefreshAll();
    }

    public int ApplyShellDamage(int incomingDamage, out int absorbedDamage, out bool shellBrokenThisHit)
    {
        absorbedDamage = 0;
        shellBrokenThisHit = false;

        if (incomingDamage <= 0 || currentShellHp <= 0)
        {
            return incomingDamage;
        }

        absorbedDamage = Mathf.Min(currentShellHp, incomingDamage);
        currentShellHp -= absorbedDamage;
        int remainingDamage = incomingDamage - absorbedDamage;

        if (absorbedDamage > 0 && currentShellHp <= 0)
        {
            currentShellHp = 0;
            shellBrokenThisHit = true;
        }

        RefreshAll();
        return remainingDamage;
    }

    private void RefreshAll()
    {
        UpdateUI();
        UpdateTurnUI();
        statusIconPresenter?.RefreshNow();
    }

    private void SyncFromComponents()
    {
        level = progression.Level;
        currentExp = progression.CurrentExp;
        attackInterval = turnState.AttackInterval;
        currentCooldown = turnState.CurrentCooldown;
    }
}
