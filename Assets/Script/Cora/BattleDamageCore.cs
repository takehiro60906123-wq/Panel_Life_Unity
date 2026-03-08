using System;
using System.Collections.Generic;

public sealed class BattleDamageRuleSet
{
    public int EvasionPercent { get; set; } = 10;
    public int CriticalPercent { get; set; } = 20;
    public int CriticalMultiplier { get; set; } = 2;
}

public sealed class DamageRollResult
{
    public bool IsMiss { get; set; }
    public bool IsCritical { get; set; }
    public int FinalDamage { get; set; }
}

public sealed class DamagePresentationResult
{
    public string DamageText { get; set; } = string.Empty;
    public string PopupKind { get; set; } = string.Empty;
}

public sealed class PlayerProgressState
{
    public int Level { get; set; } = 1;
    public int CurrentExp { get; set; }
    public int CurrentHp { get; set; } = 15;
    public int MaxHp { get; set; } = 15;
    public IReadOnlyList<int> ExpTable { get; set; } = new[] { 0, 10, 30, 60, 100, 150, 220, 300, 400, 500 };
}

public sealed class ExpGainResult
{
    public int GainedExp { get; set; }
    public bool IsLevelUp { get; set; }
    public int LevelBefore { get; set; }
    public int LevelAfter { get; set; }
    public int IncreasedMaxHpTotal { get; set; }
}

public sealed class BattleDamageCore
{
    private readonly IBattleRandom random;

    public BattleDamageCore(IBattleRandom random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public DamageRollResult ResolveDamageRoll(int baseDamage, BattleDamageRuleSet ruleSet)
    {
        if (ruleSet == null)
        {
            throw new ArgumentNullException(nameof(ruleSet));
        }

        if (baseDamage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDamage));
        }

        bool isMiss = random.Range(0, 100) < ruleSet.EvasionPercent;
        if (isMiss)
        {
            return new DamageRollResult
            {
                IsMiss = true,
                IsCritical = false,
                FinalDamage = 0,
            };
        }

        bool isCritical = random.Range(0, 100) < ruleSet.CriticalPercent;
        int finalDamage = isCritical
            ? baseDamage * ruleSet.CriticalMultiplier
            : baseDamage;

        return new DamageRollResult
        {
            IsMiss = false,
            IsCritical = isCritical,
            FinalDamage = finalDamage,
        };
    }

    public DamagePresentationResult BuildPresentation(DamageRollResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.IsMiss)
        {
            return new DamagePresentationResult
            {
                DamageText = "Miss",
                PopupKind = "Miss",
            };
        }

        if (result.IsCritical)
        {
            return new DamagePresentationResult
            {
                DamageText = $"CRITICAL!\n{result.FinalDamage}",
                PopupKind = "Critical",
            };
        }

        return new DamagePresentationResult
        {
            DamageText = result.FinalDamage.ToString(),
            PopupKind = "Normal",
        };
    }

    public ExpGainResult ApplyExp(PlayerProgressState state, int gainedExp)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.ExpTable == null || state.ExpTable.Count == 0)
        {
            throw new InvalidOperationException("ExpTable が未設定です。");
        }

        if (state.Level <= 0 || state.Level >= state.ExpTable.Count)
        {
            // 最終レベル到達済みでも経験値自体は加算しておく。
            state.CurrentExp += gainedExp;
            return new ExpGainResult
            {
                GainedExp = gainedExp,
                IsLevelUp = false,
                LevelBefore = state.Level,
                LevelAfter = state.Level,
                IncreasedMaxHpTotal = 0,
            };
        }

        state.CurrentExp += gainedExp;

        int levelBefore = state.Level;
        int hpIncreaseTotal = 0;

        while (state.Level < state.ExpTable.Count && state.CurrentExp >= state.ExpTable[state.Level])
        {
            state.Level++;
            int hpIncrease = random.Range(4, 6);
            state.MaxHp += hpIncrease;
            state.CurrentHp = state.MaxHp;
            hpIncreaseTotal += hpIncrease;

            if (state.Level >= state.ExpTable.Count)
            {
                break;
            }
        }

        return new ExpGainResult
        {
            GainedExp = gainedExp,
            IsLevelUp = state.Level > levelBefore,
            LevelBefore = levelBefore,
            LevelAfter = state.Level,
            IncreasedMaxHpTotal = hpIncreaseTotal,
        };
    }
}
