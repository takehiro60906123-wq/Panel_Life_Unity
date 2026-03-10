using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class EnemyAnimationAutoBuilderWindow : EditorWindow
{
    private DefaultAsset scanRootFolder;
    private DefaultAsset outputFolder;
    private bool overwriteExisting = true;
    private bool logDetails = true;

    private Vector2 scroll;
    private readonly List<BuildResult> results = new();

    [MenuItem("Tools/Enemy Animation Auto Builder")]
    public static void Open()
    {
        GetWindow<EnemyAnimationAutoBuilderWindow>("Enemy Anim Auto Builder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Enemy Animation Auto Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scanRootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Scan Root Folder", scanRootFolder, typeof(DefaultAsset), false);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        logDetails = EditorGUILayout.Toggle("Verbose Log", logDetails);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(scanRootFolder == null || outputFolder == null))
        {
            if (GUILayout.Button("Scan And Build"))
            {
                Build();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var result in results)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{result.creatureKey}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Set Asset: {result.assetPath}");
            EditorGUILayout.LabelField($"Confidence: {result.confidence}");

            DrawClipLine("Idle", result.set != null ? result.set.idle : null);
            DrawClipLine("Move", result.set != null ? result.set.move : null);
            DrawClipLine("Attack", result.set != null ? result.set.attack : null);
            DrawClipLine("Hit", result.set != null ? result.set.hit : null);
            DrawClipLine("Death", result.set != null ? result.set.death : null);
            DrawClipLine("Appear", result.set != null ? result.set.appear : null);
            DrawClipLine("Special", result.set != null ? result.set.special : null);

            if (result.warnings.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Warnings", EditorStyles.miniBoldLabel);
                foreach (var w in result.warnings)
                {
                    EditorGUILayout.HelpBox(w, MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawClipLine(string label, AnimationClip clip)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(label, clip, typeof(AnimationClip), false);
        }
    }

    private void Build()
    {
        results.Clear();

        string scanRootPath = AssetDatabase.GetAssetPath(scanRootFolder);
        string outputRootPath = AssetDatabase.GetAssetPath(outputFolder);

        if (!AssetDatabase.IsValidFolder(scanRootPath) || !AssetDatabase.IsValidFolder(outputRootPath))
        {
            Debug.LogError("Scan Root Folder または Output Folder が不正です。");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { scanRootPath });
        var clips = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .Select(path => new ClipInfo(path, AssetDatabase.LoadAssetAtPath<AnimationClip>(path)))
            .Where(x => x.clip != null)
            .Where(x => !ShouldIgnoreClip(x))
            .ToList();

        var groups = GroupClips(clips);

        foreach (var kv in groups.OrderBy(x => x.Key))
        {
            string creatureKey = kv.Key;
            List<ClipInfo> groupClips = kv.Value;

            EnemyAnimationSet set = CreateOrLoadSet(outputRootPath, creatureKey);
            BuildResult result = new BuildResult
            {
                creatureKey = creatureKey,
                set = set,
                assetPath = AssetDatabase.GetAssetPath(set),
            };

            AutoAssign(set, groupClips, result);

            EditorUtility.SetDirty(set);
            results.Add(result);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (logDetails)
        {
            foreach (var r in results)
            {
                Debug.Log($"[EnemyAnimAutoBuilder] {r.creatureKey} built. Confidence={r.confidence}");
            }
        }

        Debug.Log($"[EnemyAnimAutoBuilder] Done. Built {results.Count} sets.");
    }

    private EnemyAnimationSet CreateOrLoadSet(string outputRootPath, string creatureKey)
    {
        string safeName = MakeSafeFileName(creatureKey);
        string assetPath = $"{outputRootPath}/{safeName}_AnimSet.asset";

        EnemyAnimationSet existing = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>(assetPath);
        if (existing != null)
        {
            if (!overwriteExisting)
            {
                return existing;
            }

            existing.creatureKey = creatureKey;
            return existing;
        }

        EnemyAnimationSet created = ScriptableObject.CreateInstance<EnemyAnimationSet>();
        created.creatureKey = creatureKey;
        AssetDatabase.CreateAsset(created, assetPath);
        return created;
    }

    private void AutoAssign(EnemyAnimationSet set, List<ClipInfo> clips, BuildResult result)
    {
        set.idle = PickBest(clips, AnimSlot.Idle, result);
        set.move = PickBest(clips, AnimSlot.Move, result);
        set.attack = PickBest(clips, AnimSlot.Attack, result);
        set.hit = PickBest(clips, AnimSlot.Hit, result);
        set.death = PickBest(clips, AnimSlot.Death, result);
        set.appear = PickBest(clips, AnimSlot.Appear, result);
        set.special = PickBest(clips, AnimSlot.Special, result);

        int filled = 0;
        if (set.idle != null) filled++;
        if (set.move != null) filled++;
        if (set.attack != null) filled++;
        if (set.hit != null) filled++;
        if (set.death != null) filled++;
        if (set.appear != null) filled++;
        if (set.special != null) filled++;

        result.confidence = $"{filled}/7 assigned";

        if (set.idle == null) result.warnings.Add("Idle が見つかっていません。");
        if (set.move == null) result.warnings.Add("Move が見つかっていません。");
        if (set.attack == null) result.warnings.Add("Attack が見つかっていません。");
        if (set.death == null) result.warnings.Add("Death が見つかっていません。");

        if (set.hit == null)
        {
            result.warnings.Add("Hit が見つかっていません。damage/ko なしの可能性があります。");
        }

        if (set.move == null && set.idle != null)
        {
            set.move = set.idle;
            result.warnings.Add("Move がなかったため Idle を代用しました。");
        }

        if (set.appear == null && clips.Any(c => ContainsAny(c.normalizedName, "out", "return", "jump")))
        {
            set.appear = clips
                .Where(c => ContainsAny(c.normalizedName, "out", "return", "jump"))
                .OrderByDescending(c => Score(c, AnimSlot.Appear))
                .Select(c => c.clip)
                .FirstOrDefault();

            if (set.appear != null)
            {
                result.warnings.Add("Appear は out/return/jump から推定しました。");
            }
        }
    }

    private AnimationClip PickBest(List<ClipInfo> clips, AnimSlot slot, BuildResult result)
    {
        var ranked = clips
            .Select(c => new
            {
                c.clip,
                c.path,
                c.normalizedName,
                score = Score(c, slot)
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.path.Length)
            .ToList();

        if (ranked.Count == 0)
        {
            return null;
        }

        if (ranked.Count > 1 && ranked[0].score == ranked[1].score)
        {
            result.warnings.Add($"{slot} は同点候補がありました: {ranked[0].clip.name} / {ranked[1].clip.name}");
        }

        return ranked[0].clip;
    }

    private int Score(ClipInfo c, AnimSlot slot)
    {
        string n = c.normalizedName;
        string p = c.normalizedPath;

        int score = 0;

        switch (slot)
        {
            case AnimSlot.Idle:
                if (ContainsAny(n, "idle")) score += 120;
                if (ContainsAny(n, "fly") && !ContainsAny(n, "attack", "angry")) score += 40;
                if (ContainsAny(p, "idle")) score += 20;
                break;

            case AnimSlot.Move:
                if (ContainsAny(n, "walk", "move", "fly")) score += 120;
                if (ContainsAny(n, "jump", "return")) score += 50;
                if (ContainsAny(n, "angry fly")) score += 30;
                if (ContainsAny(p, "walk", "move", "fly")) score += 20;
                break;

            case AnimSlot.Attack:
                if (ContainsAny(n, "attack", "atack", "shoot", "fire")) score += 140;
                if (ContainsAny(n, "angry") && !ContainsAny(n, "fly")) score += 35;
                if (ContainsAny(p, "attack", "atack")) score += 20;
                break;

            case AnimSlot.Hit:
                if (ContainsAny(n, "damage", "hit")) score += 140;
                if (ContainsAny(n, "ko")) score += 90;
                if (ContainsAny(p, "damage", "hit")) score += 20;
                break;

            case AnimSlot.Death:
                if (ContainsAny(n, "death", "dead")) score += 160;
                if (ContainsAny(n, "ko")) score += 40;
                if (ContainsAny(p, "death", "dead")) score += 20;
                break;

            case AnimSlot.Appear:
                if (ContainsAny(n, "appear", "spawn", "out")) score += 130;
                if (ContainsAny(n, "return", "jump")) score += 60;
                if (ContainsAny(p, "appear", "spawn")) score += 20;
                break;

            case AnimSlot.Special:
                if (ContainsAny(n, "angry", "special", "skill")) score += 120;
                if (ContainsAny(n, "out", "return")) score += 20;
                break;
        }

        if (ContainsAny(n, "preview", "__preview__", "camera"))
        {
            score -= 1000;
        }

        return score;
    }

    private Dictionary<string, List<ClipInfo>> GroupClips(List<ClipInfo> clips)
    {
        var map = new Dictionary<string, List<ClipInfo>>();

        foreach (var clip in clips)
        {
            string key = GuessCreatureKey(clip);
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "Unknown";
            }

            if (!map.ContainsKey(key))
            {
                map[key] = new List<ClipInfo>();
            }

            map[key].Add(clip);
        }

        return map;
    }

    private string GuessCreatureKey(ClipInfo clip)
    {
        string[] parts = clip.path.Split('/');

        for (int i = parts.Length - 1; i >= 0; i--)
        {
            string raw = parts[i].Trim();

            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (raw.StartsWith("+")) continue;
            if (ContainsAny(raw.ToLowerInvariant(), "animation", "animations", "sprite", "sprites", "prefab", "prefabs")) continue;
            if (raw.StartsWith("2d bestiary")) continue;

            string cleaned = NormalizeCreatureName(raw);
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                return cleaned;
            }
        }

        return NormalizeCreatureName(Path.GetFileNameWithoutExtension(clip.path));
    }

    private string NormalizeCreatureName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string s = raw.ToLowerInvariant();

        s = s.Replace("bublebee", "bubblebee");
        s = s.Replace("catterpillar", "caterpillar");
        s = s.Replace("armadilo", "armadillo");
        s = s.Replace("frogs", "frog");
        s = s.Replace("bugs", "bug");
        s = s.Replace("small frogs", "small frog");
        s = s.Replace("big frogs", "big frog");

        s = Regex.Replace(s, @"\bvariant\b", "");
        s = Regex.Replace(s, @"\boriginal\b", "");
        s = Regex.Replace(s, @"\banimations?\b", "");
        s = Regex.Replace(s, @"\bsprites?\b", "");
        s = Regex.Replace(s, @"\bset\s*\d+\b", "");
        s = Regex.Replace(s, @"\s+", " ").Trim();

        if (s == "bee hive") s = "hive bee";

        return ObjectNames.NicifyVariableName(s).Replace(" ", "");
    }

    private bool ShouldIgnoreClip(ClipInfo c)
    {
        string n = c.normalizedName;
        string p = c.normalizedPath;

        if (ContainsAny(n, "__preview__", "preview")) return true;
        if (ContainsAny(p, "/camera.")) return true;
        if (ContainsAny(p, "camera.anim")) return true;

        return false;
    }

    private static bool ContainsAny(string source, params string[] words)
    {
        foreach (string w in words)
        {
            if (source.Contains(w)) return true;
        }
        return false;
    }

    private string MakeSafeFileName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            s = s.Replace(c.ToString(), "");
        }
        return s.Replace(" ", "_");
    }

    private enum AnimSlot
    {
        Idle,
        Move,
        Attack,
        Hit,
        Death,
        Appear,
        Special
    }

    private class ClipInfo
    {
        public string path;
        public string normalizedPath;
        public string normalizedName;
        public AnimationClip clip;

        public ClipInfo(string path, AnimationClip clip)
        {
            this.path = path;
            this.clip = clip;
            normalizedPath = path.ToLowerInvariant();
            normalizedName = clip.name.ToLowerInvariant();
        }
    }

    private class BuildResult
    {
        public string creatureKey;
        public string assetPath;
        public EnemyAnimationSet set;
        public string confidence;
        public readonly List<string> warnings = new();
    }
}