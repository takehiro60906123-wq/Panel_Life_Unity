using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DungeonMistController : MonoBehaviour
{
    [Header("â‡ê›íË")]
    public Transform dungeonMistRoot;
    [Range(0f, 1f)] public float battleMistAlpha = 0.8f;
    public float mistFadeDuration = 0.35f;

    private readonly List<CanvasGroup> dungeonMistPanels = new List<CanvasGroup>();
    private bool isInitialized = false;

    public void Configure(Transform root, float alpha, float fadeDuration)
    {
        dungeonMistRoot = root;
        battleMistAlpha = alpha;
        mistFadeDuration = fadeDuration;

        RefreshPanels();
    }

    public void ApplyBattleState(bool isBattle, bool immediate = false)
    {
        if (!isInitialized)
        {
            RefreshPanels();
        }

        float targetAlpha = isBattle ? battleMistAlpha : 0f;

        foreach (CanvasGroup cg in dungeonMistPanels)
        {
            if (cg == null) continue;

            cg.DOKill();

            if (immediate)
            {
                cg.alpha = targetAlpha;
            }
            else
            {
                cg.DOFade(targetAlpha, mistFadeDuration);
            }
        }
    }

    private void RefreshPanels()
    {
        dungeonMistPanels.Clear();

        if (dungeonMistRoot == null)
        {
            isInitialized = true;
            return;
        }

        foreach (Transform child in dungeonMistRoot)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = child.gameObject.AddComponent<CanvasGroup>();
            }

            cg.interactable = false;
            cg.blocksRaycasts = false;
            dungeonMistPanels.Add(cg);
        }

        isInitialized = true;
    }
}