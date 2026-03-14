using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BattleUnit にアタッチし、状態異常の付与・問い合わせ・消費・解除を管理する。
/// 個別の状態異常ロジック（行動スキップ判定など）はここには書かず、
/// 呼び出し側（BattleTurnController 等）が HasEffect() で判断して制御する。
/// </summary>
public class StatusEffectHolder : MonoBehaviour
{
    [Header("デバッグ表示")]
    [SerializeField] private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

    /// <summary>
    /// 状態異常の内容が変わった時に発火する。
    /// UI 更新用途。
    /// </summary>
    public event Action OnEffectsChanged;

    public void ApplyEffect(StatusEffectType type, int turns, bool removeOnDamage, int potency = 0)
    {
        if (type == StatusEffectType.None) return;
        if (turns <= 0) return;

        StatusEffectInstance existing = FindActiveEffect(type);

        if (existing != null)
        {
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
            existing.removeOnDamage = existing.removeOnDamage || removeOnDamage;
            existing.potency = Mathf.Max(existing.potency, potency);
            Debug.Log($"[StatusEffect] {gameObject.name}: {type} リフレッシュ (残り{existing.remainingTurns}T, potency={existing.potency})");
            NotifyChanged();
            return;
        }

        StatusEffectInstance instance = new StatusEffectInstance(type, turns, removeOnDamage, potency);
        activeEffects.Add(instance);
        Debug.Log($"[StatusEffect] {gameObject.name}: {type} 付与 ({turns}T, potency={potency})");
        NotifyChanged();
    }

    public bool HasEffect(StatusEffectType type)
    {
        return FindActiveEffect(type) != null;
    }

    public int GetRemainingTurns(StatusEffectType type)
    {
        StatusEffectInstance effect = FindActiveEffect(type);
        return effect != null ? effect.remainingTurns : 0;
    }

    public int GetEffectPotency(StatusEffectType type)
    {
        StatusEffectInstance effect = FindActiveEffect(type);
        return effect != null ? effect.potency : 0;
    }

    public int ActiveCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].IsActive())
                {
                    count++;
                }
            }
            return count;
        }
    }

    public void ConsumeEffectTurn(StatusEffectType type)
    {
        StatusEffectInstance effect = FindActiveEffect(type);
        if (effect == null) return;

        if (effect.TickAndCheckExpired())
        {
            activeEffects.Remove(effect);
            Debug.Log($"[StatusEffect] {gameObject.name}: {type} 自然解除");
        }
        else
        {
            Debug.Log($"[StatusEffect] {gameObject.name}: {type} 残り{effect.remainingTurns}T");
        }

        NotifyChanged();
    }

    public void OnDamageReceived()
    {
        bool removedAny = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].removeOnDamage)
            {
                Debug.Log($"[StatusEffect] {gameObject.name}: {activeEffects[i].type} 被弾解除");
                activeEffects.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            NotifyChanged();
        }
    }

    public void RemoveEffect(StatusEffectType type)
    {
        bool removedAny = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].type == type)
            {
                Debug.Log($"[StatusEffect] {gameObject.name}: {type} 手動解除");
                activeEffects.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            NotifyChanged();
        }
    }

    public void ClearAll()
    {
        if (activeEffects.Count > 0)
        {
            Debug.Log($"[StatusEffect] {gameObject.name}: 全状態異常クリア ({activeEffects.Count}件)");
            activeEffects.Clear();
            NotifyChanged();
        }
    }

    private StatusEffectInstance FindActiveEffect(StatusEffectType type)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].type == type && activeEffects[i].IsActive())
            {
                return activeEffects[i];
            }
        }

        return null;
    }

    private void NotifyChanged()
    {
        OnEffectsChanged?.Invoke();
    }
}
