using UnityEngine;

public class BattleBackgroundThemeController : MonoBehaviour
{
    [System.Serializable]
    public class ThemeEntry
    {
        public string themeName;
        public int startFloor = 1;
        public int endFloor = 4;
        public GameObject root;
        public float yOffset = 0f;
    }

    [Header("Theme Roots")]
    [SerializeField] private ThemeEntry[] themes;

    [Header("Preview / Fallback")]
    [SerializeField] private int previewFloor = 1;
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool disableAllWhenNoMatch = false;

    private int currentAppliedFloor = -1;
    private int currentThemeIndex = -1;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyTheme(previewFloor);
        }
    }

    public void ApplyTheme(int floor)
    {
        currentAppliedFloor = floor;
        currentThemeIndex = FindThemeIndex(floor);

        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i].root == null)
            {
                continue;
            }

            bool active = (i == currentThemeIndex);
            if (currentThemeIndex < 0 && !disableAllWhenNoMatch)
            {
                active = (i == 0);
            }

            if (themes[i].root.activeSelf != active)
            {
                themes[i].root.SetActive(active);
            }

            if (active)
            {
                ApplyThemeYOffset(themes[i]);
            }
        }

        ResetActiveThemeLoopPositions();
    }

    public void ApplyThemeFromBattleNumber(int battleNumber)
    {
        ApplyTheme(Mathf.Max(1, battleNumber));
    }

    public int GetCurrentAppliedFloor()
    {
        return currentAppliedFloor;
    }

    public bool WouldThemeChange(int floor)
    {
        return GetThemeIndexForFloor(floor) != currentThemeIndex;
    }

    public int GetThemeIndexForFloor(int floor)
    {
        return FindThemeIndex(floor);
    }

    public string GetCurrentThemeName()
    {
        if (currentThemeIndex < 0 || currentThemeIndex >= themes.Length)
        {
            return string.Empty;
        }

        return themes[currentThemeIndex].themeName;
    }

    private int FindThemeIndex(int floor)
    {
        for (int i = 0; i < themes.Length; i++)
        {
            ThemeEntry entry = themes[i];
            if (entry.root == null)
            {
                continue;
            }

            if (floor >= entry.startFloor && floor <= entry.endFloor)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyThemeYOffset(ThemeEntry entry)
    {
        if (entry.root == null)
        {
            return;
        }

        Transform rootTransform = entry.root.transform;
        Vector3 localPos = rootTransform.localPosition;
        localPos.y = entry.yOffset;
        rootTransform.localPosition = localPos;
    }

    private void ResetActiveThemeLoopPositions()
    {
        if (currentThemeIndex < 0 || currentThemeIndex >= themes.Length)
        {
            return;
        }

        GameObject activeRoot = themes[currentThemeIndex].root;
        if (activeRoot == null)
        {
            return;
        }

        InfiniteBackground[] loops = activeRoot.GetComponentsInChildren<InfiniteBackground>(true);
        for (int i = 0; i < loops.Length; i++)
        {
            loops[i].ResetToInitialPosition();
        }
    }
}
