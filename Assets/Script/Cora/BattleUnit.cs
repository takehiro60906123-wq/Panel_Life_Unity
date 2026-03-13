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

    [Header("報酬（敵専用）")]
    public int expYield = 2;

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

    [Header("アニメーター")]
    public Animator animator;

    [Header("行動ターン（敵専用 / 互換用）")]
    public int attackInterval = 1;
    [HideInInspector] public int currentCooldown;
    public TextMeshProUGUI turnText;

    private PlayerProgression progression;
    private EnemyTurnState turnState;
    private BattleUnitView view;

    public int CurrentHP => currentHP;
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

        progression = GetComponent<PlayerProgression>();
        turnState = GetComponent<EnemyTurnState>();
        view = GetComponent<BattleUnitView>();

        progression.Initialize(level, currentExp);
        turnState.Configure(attackInterval, currentCooldown);
        view.BindLegacyReferences(hpSlider, hpText, levelText, turnText, animator);

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
        view.RefreshHP(currentHP, maxHP);
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
        view.PlayIdle();
        RefreshAll();
    }

    public void SetUIActive(bool isActive)
    {
        view.SetUIActive(isActive);
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

    // =============================================================
    // モンスターパネルによる敵レベルアップ
    // =============================================================

    public struct EnemyLevelUpResult
    {
        public int levelsGained;
        public int hpGained;
        public int healAmount;
        public int expBonusGained;
        public int newEnemyLevel;
    }

    /// <summary>
    /// モンスターパネルによる敵レベルアップ。
    /// chainCount = 消したモンスターパネルの連結数。
    /// </summary>
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

    // =============================================================

    private void RefreshAll()
    {
        UpdateUI();
        UpdateTurnUI();
    }

    private void SyncFromComponents()
    {
        level = progression.Level;
        currentExp = progression.CurrentExp;
        attackInterval = turnState.AttackInterval;
        currentCooldown = turnState.CurrentCooldown;
    }
}