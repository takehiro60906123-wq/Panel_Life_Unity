using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Animation Set")]
public class EnemyAnimationSet : ScriptableObject
{
    public string creatureKey;

    public AnimationClip idle;
    public AnimationClip move;
    public AnimationClip attack;
    public AnimationClip hit;
    public AnimationClip death;
    public AnimationClip appear;
    public AnimationClip special;
}