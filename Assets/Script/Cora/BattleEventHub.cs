using System;
using UnityEngine;

public class BattleEventHub : MonoBehaviour
{
    public event Action<bool> BoardInteractableRequested;
    public event Action<int> CoinsGained;
    public event Action<int> EnemyDamageRequested;
    public event Action<Vector3, Vector3, float, float> EnergyOrbRequested;

    // ============================================
    // 弾薬パネル収集イベント（旧 MagicBulletRequested を置換）
    // panelCount = 消したAmmoパネルの枚数
    // ============================================
    public event Action<int> AmmoCollected;

    // ============================================
    // 攻撃パネル消去時のおやつゲージ加算イベント
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

    public void RaiseEnergyOrbRequested(Vector3 startPos, Vector3 targetPos, float duration, float delay)
    {
        EnergyOrbRequested?.Invoke(startPos, targetPos, duration, delay);
    }

    /// <summary>
    /// 弾薬パネル消去時に呼ぶ。panelCount = 消した枚数。
    /// PanelBattleManager 側で枚数 × ammoGaugePerPanel を加算する。
    /// </summary>
    public void RaiseAmmoCollected(int panelCount)
    {
        AmmoCollected?.Invoke(panelCount);
    }

    /// <summary>
    /// 攻撃パネル（Sword）消去時に呼ぶ。
    /// PanelBattleManager 側で swordGaugeBonusPerAction (+1) を加算する。
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