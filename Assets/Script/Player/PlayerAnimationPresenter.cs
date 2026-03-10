using UnityEngine;

public class PlayerAnimationPresenter : MonoBehaviour
{
    [Header("Q")]
    [SerializeField] private Animator visualAnimator;

    [Header("State")]
    [SerializeField] private string idleStateName = "IDLE";
    [SerializeField] private string runStateName = "RUN";
    [SerializeField] private string runShootStateName = "RUN_SHOOT";
    [SerializeField] private string hurtStateName = "HURT";
    [SerializeField] private string spinStateName = "SPIN";

    [Header("遷移")]
    [SerializeField] private float crossFadeDuration = 0.05f;
    [SerializeField] private float idleCrossFadeDuration = 0.15f;

    private int lastRequestedStateHash = 0;
    private int idleStateHash;

    private void Awake()
    {
        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<Animator>(true);
        }
        idleStateHash = Animator.StringToHash(idleStateName);
    }

    public void PlayIdle()
    {
        TryPlayState(idleStateName);
    }

    public void PlayRun()
    {
        TryPlayState(runStateName);
    }

    public void PlayRunShoot()
    {
        TryPlayState(runShootStateName);
    }

    public void PlayHurt()
    {
        TryPlayState(hurtStateName, true);
    }

    public void PlaySpin()
    {
        TryPlayState(spinStateName, true);
    }

    private bool TryPlayState(string stateName, bool forceRestart = false)
    {
        if (visualAnimator == null) return false;
        if (string.IsNullOrEmpty(stateName)) return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!visualAnimator.HasState(0, stateHash))
        {
            return false;
        }

        // 同じステートをリクエスト済みならスキップ（遷移中の二重発火を防止）
        if (!forceRestart && lastRequestedStateHash == stateHash)
        {
            return true;
        }

        // IDLE へはゆっくりブレンドし、RUN→IDLE のポーズ急変を緩和
        float duration = (stateHash == idleStateHash) ? idleCrossFadeDuration : crossFadeDuration;
        visualAnimator.CrossFadeInFixedTime(stateHash, duration, 0);
        lastRequestedStateHash = stateHash;
        return true;
    }

    public Animator GetAnimator()
    {
        return visualAnimator;
    }
}