using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FootstepLoopPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Min(0.02f)] private float interval = 0.22f;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

    private Coroutine loopRoutine;
    private bool isLooping;
    private int lastIndex = -1;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDisable()
    {
        EndLoop();
    }

    public void BeginLoop()
    {
        if (isLooping) return;
        if (!CanPlay()) return;

        isLooping = true;
        loopRoutine = StartCoroutine(FootstepLoopRoutine());
    }

    public void EndLoop()
    {
        isLooping = false;

        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }
    }

    public void PlayOneShot()
    {
        if (!CanPlay()) return;
        PlayRandomClip();
    }

    private IEnumerator FootstepLoopRoutine()
    {
        while (isLooping)
        {
            PlayRandomClip();
            yield return new WaitForSeconds(interval);
        }

        loopRoutine = null;
    }

    private void PlayRandomClip()
    {
        if (audioSource == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = 0;
        if (footstepClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = Random.Range(0, footstepClips.Length);
            }
            while (index == lastIndex);
        }

        lastIndex = index;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(footstepClips[index], volume);
    }

    private bool CanPlay()
    {
        return audioSource != null
            && footstepClips != null
            && footstepClips.Length > 0;
    }
}