using UnityEngine;

public class BattleContext : MonoBehaviour
{
    [Header("戦闘対象")]
    [SerializeField] private BattleUnit currentEnemy;

    [Header("進行状態")]
    [SerializeField] private EncounterType currentEncounter = EncounterType.Enemy;
    [SerializeField] private int remainingSteps = 0;
    [SerializeField] private int currentCoins = 0;

    [Header("フラグ")]
    [SerializeField] private bool isPlayerTurn = true;
    [SerializeField] private bool isEnemySpawning = false;
    [SerializeField] private bool isEnemyDefeatedThisTurn = false;

    public BattleUnit CurrentEnemy
    {
        get => currentEnemy;
        set => currentEnemy = value;
    }

    public EncounterType CurrentEncounter
    {
        get => currentEncounter;
        set => currentEncounter = value;
    }

    public int RemainingSteps
    {
        get => remainingSteps;
        set => remainingSteps = value;
    }

    public int CurrentCoins
    {
        get => currentCoins;
        set => currentCoins = value;
    }

    public bool IsPlayerTurn
    {
        get => isPlayerTurn;
        set => isPlayerTurn = value;
    }

    public bool IsEnemySpawning
    {
        get => isEnemySpawning;
        set => isEnemySpawning = value;
    }

    public bool IsEnemyDefeatedThisTurn
    {
        get => isEnemyDefeatedThisTurn;
        set => isEnemyDefeatedThisTurn = value;
    }

    public void ResetRuntimeFlags()
    {
        isPlayerTurn = true;
        isEnemySpawning = false;
        isEnemyDefeatedThisTurn = false;
    }
}
