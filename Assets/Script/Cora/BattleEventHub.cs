using System;
using UnityEngine;

public class BattleEventHub : MonoBehaviour
{
    public event Action<bool> BoardInteractableRequested;
    public event Action<int> CoinsGained;
    public event Action<int> EnemyDamageRequested;
    public event Action<PanelType, Vector3, Vector3, float, float> EnergyOrbRequested;

    // ============================================
    // eplWCxgi MagicBulletRequested uj
    // panelCount = Ammopl̖
    // ============================================
    public event Action<int> AmmoCollected;

    // ============================================
    // Upl̂Q[WZCxg
    // ============================================
    public event Action SwordBonusGaugeRequested;

    public event Action<EncounterType, int> EncounterStateChanged;
    public event Action<bool, bool> DungeonMistRequested;
    public event Action<string, Vector3, Color> DamageTextRequested;
    public event Action<GameObject, Vector3, float> OneShotEffectRequested;
    public event Action<int, Vector3, float> ExpTextRequested;
    public event Action<float> LevelUpTextRequested;
    public event Action StageClearRequested;
    public event Action PlayerDefeatedRequested;
    public event Action<BattleUnit> EnemyDefeated;

    public void RaiseBoardInteractableRequested(bool isInteractable)
    {
        BoardInteractableRequested?.Invoke(isInteractable);
    }

    public void RaiseCoinsGained(int amount)
    {
        CoinsGained?.Invoke(amount);
    }

    public void RaiseEnemyDamageRequested(int baseDamage)
    {
        EnemyDamageRequested?.Invoke(baseDamage);
    }

    public void RaiseEnergyOrbRequested(PanelType panelType, Vector3 startPos, Vector3 targetPos, float duration, float delay)
    {
        EnergyOrbRequested?.Invoke(panelType, startPos, targetPos, duration, delay);
    }

    /// <summary>
    /// eplɌĂԁBpanelCount = B
    /// PanelBattleManager Ŗ ~ ammoGaugePerPanel ZB
    /// </summary>
    public void RaiseAmmoCollected(int panelCount)
    {
        AmmoCollected?.Invoke(panelCount);
    }

    /// <summary>
    /// UpliSwordjɌĂԁB
    /// PanelBattleManager  swordGaugeBonusPerAction (+1) ZB
    /// </summary>
    public void RaiseSwordBonusGaugeRequested()
    {
        SwordBonusGaugeRequested?.Invoke();
    }

    public void RaiseEncounterStateChanged(EncounterType encounterType, int remainingSteps)
    {
        EncounterStateChanged?.Invoke(encounterType, remainingSteps);
    }

    public void RaiseDungeonMistRequested(bool isBattle, bool immediate)
    {
        DungeonMistRequested?.Invoke(isBattle, immediate);
    }

    public void RaiseDamageTextRequested(string text, Vector3 position, Color color)
    {
        DamageTextRequested?.Invoke(text, position, color);
    }

    public void RaiseOneShotEffectRequested(GameObject prefab, Vector3 position, float returnDelay)
    {
        OneShotEffectRequested?.Invoke(prefab, position, returnDelay);
    }

    public void RaiseExpTextRequested(int exp, Vector3 position, float delay)
    {
        ExpTextRequested?.Invoke(exp, position, delay);
    }

    public void RaiseLevelUpTextRequested(float delay)
    {
        LevelUpTextRequested?.Invoke(delay);
    }

    public void RaiseStageClearRequested()
    {
        StageClearRequested?.Invoke();
    }

    public void RaisePlayerDefeatedRequested()
    {
        PlayerDefeatedRequested?.Invoke();
    }
    public void RaiseEnemyDefeated(BattleUnit defeatedEnemy)
    {
        EnemyDefeated?.Invoke(defeatedEnemy);
    }
}