using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleUnit の状態異常を HPバー左下のアイコン群へ反映する。
/// BattleUnit 側から自動追加される前提の軽量 Presenter。
/// 
/// 想定構造:
///   BattleUnit
///     └ Canvas
///         └ StatusIconRoot
///             ├ Image
///             ├ Image
///             └ Image
/// 
/// - StatusIconRoot は BattleUnit 配下のどこでもよいが、名前は "StatusIconRoot" を推奨。
/// - 子の Image は 1段横並び 3個を想定。
/// - 子 Image が無い場合は簡易スロットを自動生成する。
/// </summary>
[DisallowMultipleComponent]
public class BattleStatusIconPresenter : MonoBehaviour
{
    [Header("表示先")]
    [SerializeField] private RectTransform statusIconRoot;
    [SerializeField] private List<Image> iconSlots = new List<Image>();

    [Header("自動生成")]
    [SerializeField] private bool autoCreateSlotsIfMissing = true;
    [SerializeField, Min(1)] private int autoCreateSlotCount = 3;
    [SerializeField] private Vector2 autoCreateSlotSize = new Vector2(16f, 16f);
    [SerializeField] private float autoCreateSlotSpacing = 2f;

    [Header("アイコン設定")]
    [SerializeField] private Sprite paralysisSprite;
    [SerializeField] private Sprite slowSprite;
    [SerializeField] private Sprite corrosionSprite;
    [SerializeField] private Color paralysisColor = new Color(0.85f, 0.45f, 1.0f, 1f);
    [SerializeField] private Color slowColor = new Color(0.45f, 0.90f, 1.0f, 1f);
    [SerializeField] private Color corrosionColor = new Color(0.65f, 1.0f, 0.25f, 1f);

    private BattleUnit battleUnit;
    private StatusEffectHolder holder;
    private bool uiVisible = true;
    private bool initialized;
    private bool lastHasAnyEffects;
    private static Sprite cachedBuiltinSprite;

    public void Initialize(BattleUnit unit, StatusEffectHolder statusHolder)
    {
        battleUnit = unit != null ? unit : GetComponent<BattleUnit>();
        holder = statusHolder != null ? statusHolder : (battleUnit != null ? battleUnit.StatusEffects : GetComponent<StatusEffectHolder>());

        if (initialized)
        {
            Unsubscribe();
        }

        ResolveReferences();
        Subscribe();
        initialized = true;
        RefreshNow();
    }

    private void Awake()
    {
        if (!initialized)
        {
            Initialize(GetComponent<BattleUnit>(), GetComponent<StatusEffectHolder>());
        }
    }

    private void OnEnable()
    {
        if (initialized)
        {
            Subscribe();
            RefreshNow();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetVisible(bool isVisible)
    {
        uiVisible = isVisible;
        ApplyRootVisibility(lastHasAnyEffects);
    }

    public void RefreshNow()
    {
        ResolveReferences();

        if (statusIconRoot == null || holder == null)
        {
            return;
        }

        int slotIndex = 0;

        if (holder.HasEffect(StatusEffectType.Paralysis) && slotIndex < iconSlots.Count)
        {
            SetupSlot(iconSlots[slotIndex++], StatusEffectType.Paralysis);
        }

        if (holder.HasEffect(StatusEffectType.Slow) && slotIndex < iconSlots.Count)
        {
            SetupSlot(iconSlots[slotIndex++], StatusEffectType.Slow);
        }

        if (holder.HasEffect(StatusEffectType.Corrosion) && slotIndex < iconSlots.Count)
        {
            SetupSlot(iconSlots[slotIndex++], StatusEffectType.Corrosion);
        }

        for (int i = slotIndex; i < iconSlots.Count; i++)
        {
            if (iconSlots[i] != null)
            {
                iconSlots[i].gameObject.SetActive(false);
            }
        }

        lastHasAnyEffects = slotIndex > 0;
        ApplyRootVisibility(lastHasAnyEffects);
    }

    private void HandleEffectsChanged()
    {
        RefreshNow();
    }

    private void ApplyRootVisibility(bool hasAnyEffects)
    {
        if (statusIconRoot == null) return;
        statusIconRoot.gameObject.SetActive(uiVisible && hasAnyEffects);
    }

    private void ResolveReferences()
    {
        if (battleUnit == null)
        {
            battleUnit = GetComponent<BattleUnit>();
        }

        if (holder == null)
        {
            holder = battleUnit != null ? battleUnit.StatusEffects : GetComponent<StatusEffectHolder>();
        }

        if (statusIconRoot == null)
        {
            statusIconRoot = FindStatusIconRoot();
        }

        if (statusIconRoot != null)
        {
            CollectIconSlots();

            if (iconSlots.Count == 0 && autoCreateSlotsIfMissing)
            {
                CreateDefaultSlots();
                CollectIconSlots();
            }
        }
    }

    private void CollectIconSlots()
    {
        if (statusIconRoot == null) return;
        if (iconSlots.Count > 0) return;

        for (int i = 0; i < statusIconRoot.childCount; i++)
        {
            Transform child = statusIconRoot.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                iconSlots.Add(image);
            }
        }
    }

    private void CreateDefaultSlots()
    {
        if (statusIconRoot == null) return;

        for (int i = 0; i < autoCreateSlotCount; i++)
        {
            GameObject slot = new GameObject($"StatusIcon_{i}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(statusIconRoot, false);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = autoCreateSlotSize;
            rect.anchoredPosition = new Vector2((autoCreateSlotSize.x + autoCreateSlotSpacing) * i, 0f);

            Image image = slot.GetComponent<Image>();
            image.sprite = GetFallbackSprite();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            image.preserveAspect = true;
            image.gameObject.SetActive(false);
        }
    }

    private void SetupSlot(Image image, StatusEffectType effectType)
    {
        if (image == null) return;

        Sprite effectSprite = GetEffectSprite(effectType);
        if (effectSprite != null)
        {
            image.sprite = effectSprite;
            image.color = Color.white;
        }
        else
        {
            if (image.sprite == null)
            {
                image.sprite = GetFallbackSprite();
            }
            image.color = GetEffectColor(effectType);
        }

        image.preserveAspect = true;
        image.gameObject.SetActive(true);
    }

    private Sprite GetEffectSprite(StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectType.Paralysis:
                return paralysisSprite;
            case StatusEffectType.Slow:
                return slowSprite;
            case StatusEffectType.Corrosion:
                return corrosionSprite;
            default:
                return null;
        }
    }

    private Color GetEffectColor(StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectType.Paralysis:
                return paralysisColor;
            case StatusEffectType.Slow:
                return slowColor;
            case StatusEffectType.Corrosion:
                return corrosionColor;
            default:
                return Color.white;
        }
    }

    private RectTransform FindStatusIconRoot()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "StatusIconRoot")
            {
                return children[i] as RectTransform;
            }
        }

        return null;
    }

    private void Subscribe()
    {
        if (holder != null)
        {
            holder.OnEffectsChanged -= HandleEffectsChanged;
            holder.OnEffectsChanged += HandleEffectsChanged;
        }
    }

    private void Unsubscribe()
    {
        if (holder != null)
        {
            holder.OnEffectsChanged -= HandleEffectsChanged;
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (cachedBuiltinSprite == null)
        {
            cachedBuiltinSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        return cachedBuiltinSprite;
    }
}
