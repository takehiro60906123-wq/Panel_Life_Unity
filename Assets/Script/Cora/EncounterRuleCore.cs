using System;

public enum EncounterKind
{
    Enemy,
    Empty,
    Treasure,
    Shop,
}

public sealed class EncounterRuleConfig
{
    public int EnemyWeight { get; set; } = 65;
    public int EmptyWeight { get; set; } = 15;
    public int TreasureWeight { get; set; } = 10;
    public int ShopWeight { get; set; } = 10;

    public int EmptyMinSteps { get; set; } = 2;
    public int EmptyMaxStepsInclusive { get; set; } = 4;

    public int TreasureMinSteps { get; set; } = 1;
    public int TreasureMaxStepsInclusive { get; set; } = 2;

    // 商店は固定1ターン（買い物して立ち去る）
    public int ShopSteps { get; set; } = 1;
}

public sealed class EncounterDecisionContext
{
    public EncounterKind PreviousEncounter { get; set; } = EncounterKind.Enemy;
    public int DefeatedEnemyCount { get; set; }
    public int MaxEnemyCount { get; set; } = 100;
}

public sealed class EncounterDecisionResult
{
    public bool IsStageClear { get; set; }
    public EncounterKind Encounter { get; set; } = EncounterKind.Enemy;
    public int Steps { get; set; }
}

public sealed class EncounterRuleCore
{
    private readonly IBattleRandom random;

    public EncounterRuleCore(IBattleRandom random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public EncounterDecisionResult DecideNext(EncounterDecisionContext context, EncounterRuleConfig config)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (context.MaxEnemyCount > 0 && context.DefeatedEnemyCount >= context.MaxEnemyCount)
        {
            return new EncounterDecisionResult
            {
                IsStageClear = true,
                Encounter = EncounterKind.Enemy,
                Steps = 0,
            };
        }

        int totalWeight = config.EnemyWeight + config.EmptyWeight + config.TreasureWeight + config.ShopWeight;
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("遭遇テーブルの重み合計が 0 以下です。");
        }

        int roll = random.Range(0, totalWeight);

        if (roll < config.EnemyWeight)
        {
            return new EncounterDecisionResult
            {
                IsStageClear = false,
                Encounter = EncounterKind.Enemy,
                Steps = 0,
            };
        }

        roll -= config.EnemyWeight;
        if (roll < config.EmptyWeight)
        {
            return new EncounterDecisionResult
            {
                IsStageClear = false,
                Encounter = EncounterKind.Empty,
                Steps = random.Range(config.EmptyMinSteps, config.EmptyMaxStepsInclusive + 1),
            };
        }

        roll -= config.EmptyWeight;
        if (roll < config.TreasureWeight)
        {
            return new EncounterDecisionResult
            {
                IsStageClear = false,
                Encounter = EncounterKind.Treasure,
                Steps = random.Range(config.TreasureMinSteps, config.TreasureMaxStepsInclusive + 1),
            };
        }

        // --- 商店 ---
        return new EncounterDecisionResult
        {
            IsStageClear = false,
            Encounter = EncounterKind.Shop,
            Steps = config.ShopSteps,
        };
    }
}