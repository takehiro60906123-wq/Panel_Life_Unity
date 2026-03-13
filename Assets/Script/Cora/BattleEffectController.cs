using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class BattleEffectController : MonoBehaviour
{
    private EffectPoolManager effectPoolManager;

    public void Configure(EffectPoolManager poolManager)
    {
        effectPoolManager = poolManager;
    }

    public void SpawnDamageText(GameObject damageTextPrefab, string text, Vector3 position, Color color)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = GetPooledObject(damageTextPrefab, position, Quaternion.identity);
        if (textObj == null) return;

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
        {
            ReturnPooledObject(damageTextPrefab, textObj);
            return;
        }

        textObj.transform.position = position;
        textObj.transform.localScale = Vector3.one;

        Color baseColor = color;
        baseColor.a = 1f;

        tmp.text = text;
        tmp.color = baseColor;
        tmp.alpha = 1f;

        float randomX = UnityEngine.Random.Range(-0.5f, 0.5f);
        Vector3 targetPos = new Vector3(position.x + randomX, position.y + 1.5f, position.z);

        Sequence seq = DOTween.Sequence();
        seq.Join(textObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutCirc));
        seq.Insert(0.2f, tmp.DOFade(0f, 0.8f));
        seq.OnComplete(() =>
        {
            ReturnPooledObject(damageTextPrefab, textObj);
        });
    }

    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay)
    {
        if (prefab == null) return;

        GameObject effectObj = GetPooledObject(prefab, position, rotation);
        if (effectObj == null) return;

        StartCoroutine(ReturnPooledObjectAfterDelay(prefab, effectObj, returnDelay));
    }

    public void SpawnMagicBullet(GameObject magicBulletPrefab, Vector3 start, Vector3 target, Action onHit)
    {
        if (magicBulletPrefab == null) return;

        GameObject bullet = GetPooledObject(magicBulletPrefab, start, Quaternion.identity);
        if (bullet == null) return;

        bullet.transform.DOMove(target, 0.15f).SetEase(Ease.Linear).OnComplete(() =>
        {
            ReturnPooledObject(magicBulletPrefab, bullet);
            onHit?.Invoke();
        });
    }

    public void SpawnEnergyOrb(
        GameObject energyOrbPrefab,
        GameObject absorbEffectPrefab,
        Vector3 startPos,
        Vector3 target,
        float duration,
        float delay)
    {
        if (energyOrbPrefab == null) return;

        GameObject orb = GetPooledObject(energyOrbPrefab, startPos, Quaternion.identity);
        if (orb == null) return;

        orb.transform.DOMove(target, duration)
            .SetDelay(delay)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                ReturnPooledObject(energyOrbPrefab, orb);

                if (absorbEffectPrefab != null)
                {
                    SpawnOneShotEffect(absorbEffectPrefab, target, Quaternion.identity, 0.8f);
                }
            });
    }

    public IEnumerator SpawnExpTextWithDelay(GameObject damageTextPrefab, int exp, Vector3 spawnPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnDamageText(damageTextPrefab, $"+{exp} EXP", spawnPos, Color.green);
    }

    public IEnumerator SpawnLevelUpTextWithDelay(GameObject damageTextPrefab, Transform targetTransform, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetTransform == null) yield break;

        SpawnDamageText(
            damageTextPrefab,
            "LEVEL UP!",
            targetTransform.position + Vector3.up * 1.5f,
            Color.yellow);
    }

    // ============================================
    // アイテムGET飛行オーブ
    // パネル位置からインベントリスロットへ弧を描いて飛ぶ。
    // ============================================

    public void SpawnItemGetOrb(
        GameObject orbPrefab,
        GameObject absorbEffectPrefab,
        Vector3 startPos,
        Vector3 targetPos,
        float duration,
        System.Action onArrive)
    {
        if (orbPrefab == null)
        {
            onArrive?.Invoke();
            return;
        }

        GameObject orb = GetPooledObject(orbPrefab, startPos, Quaternion.identity);
        if (orb == null)
        {
            onArrive?.Invoke();
            return;
        }

        // 中間点を上方向にオフセットして弧を描く
        Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.3f) + Vector3.up * 0.8f;
        Vector3[] path = new Vector3[] { startPos, midPoint, targetPos };

        orb.transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                ReturnPooledObject(orbPrefab, orb);

                if (absorbEffectPrefab != null)
                {
                    SpawnOneShotEffect(absorbEffectPrefab, targetPos, Quaternion.identity, 0.6f);
                }

                onArrive?.Invoke();
            });
    }

    private GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (effectPoolManager == null)
        {
            return Instantiate(prefab, position, rotation);
        }

        return effectPoolManager.GetPooledObject(prefab, position, rotation);
    }

    private void ReturnPooledObject(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        if (effectPoolManager == null)
        {
            Destroy(obj);
            return;
        }

        effectPoolManager.ReturnPooledObject(prefab, obj);
    }

    private IEnumerator ReturnPooledObjectAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        if (obj == null) yield break;

        if (effectPoolManager == null)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
            yield break;
        }

        yield return effectPoolManager.ReturnPooledObjectAfterDelay(prefab, obj, delay);
    }
}