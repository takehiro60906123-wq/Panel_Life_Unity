using UnityEngine;

public class EnemyTurnState : MonoBehaviour
{
    [SerializeField] private int attackInterval = 1;
    [SerializeField] private int currentCooldown = 0;

    public int AttackInterval => attackInterval;
    public int CurrentCooldown => currentCooldown;

    public void Configure(int interval, int cooldown)
    {
        attackInterval = Mathf.Max(1, interval);
        currentCooldown = Mathf.Max(0, cooldown);
    }

    public void SetAttackInterval(int value)
    {
        attackInterval = Mathf.Max(1, value);
    }

    public void InitializeTurn(int initialCooldownOverride = -1)
    {
        currentCooldown = initialCooldownOverride >= 0
            ? Mathf.Max(0, initialCooldownOverride)
            : attackInterval;
    }

    public void TickDown()
    {
        if (currentCooldown > 0)
        {
            currentCooldown--;
        }
    }

    public void ResetCooldown()
    {
        currentCooldown = attackInterval;
    }

    public void Delay(int amount)
    {
        if (amount > 0)
        {
            currentCooldown += amount;
        }
    }

    public bool IsReadyToAttack()
    {
        return currentCooldown <= 0;
    }
}
