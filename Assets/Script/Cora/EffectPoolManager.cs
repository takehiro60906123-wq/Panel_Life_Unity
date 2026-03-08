using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class EffectPoolManager : MonoBehaviour
{
    private readonly Dictionary<GameObject, Queue<GameObject>> objectPools = new Dictionary<GameObject, Queue<GameObject>>();

    public GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!objectPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            objectPools.Add(prefab, pool);
        }

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }

        PrepareObject(obj, prefab, position, rotation);
        return obj;
    }

    public void ReturnPooledObject(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null) return;

        if (!objectPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            objectPools.Add(prefab, pool);
        }

        KillPooledTweens(obj);
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    public IEnumerator ReturnPooledObjectAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnPooledObject(prefab, obj);
    }

    private void PrepareObject(GameObject obj, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (obj == null) return;

        KillPooledTweens(obj);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.localScale = prefab.transform.localScale;
        obj.SetActive(true);

        TrailRenderer[] trails = obj.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var trail in trails)
        {
            trail.Clear();
        }

        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        Animator[] animators = obj.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private void KillPooledTweens(GameObject obj)
    {
        if (obj == null) return;

        obj.transform.DOKill(true);

        TMP_Text[] texts = obj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts) t.DOKill(true);

        SpriteRenderer[] srs = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.DOKill(true);

        Image[] images = obj.GetComponentsInChildren<Image>(true);
        foreach (var img in images) img.DOKill(true);

        CanvasGroup[] cgs = obj.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in cgs) cg.DOKill(true);
    }
}