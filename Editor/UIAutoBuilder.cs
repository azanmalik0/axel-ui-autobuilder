using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class UIAutoBuilder : EditorWindow
{
    private string jsonInput = "";
    private Vector2 scrollPos;

    // Settings
    private TMP_FontAsset defaultFont;
    private string assetSearchPath = "Assets";
    private bool autoMatchSprites = true;
    private bool useNativeSize = true;
    private bool useFuzzyMatch = true;

    // Sprite Tools
    private int activeTab = 0;
    private Vector2 manifestScrollPos;
    private string manifestPreview = "";

    [MenuItem("Tools/Universal UI/Auto-Builder Pro")]
    public static void ShowWindow()
    {
        GetWindow<UIAutoBuilder>("UI Auto-Builder Pro");
    }

    private void OnGUI()
    {
        GUILayout.Label("Universal UI Auto-Builder Pro", EditorStyles.boldLabel);

        activeTab = GUILayout.Toolbar(activeTab, new string[] { "Manual JSON", "Sprite Tools" });

        EditorGUILayout.Space();

        if (activeTab == 0) DrawManualTab();
        else DrawSpriteToolsTab();
    }

    private void DrawManualTab()
    {
        defaultFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Default TMP Font", defaultFont, typeof(TMP_FontAsset), false);
        assetSearchPath = EditorGUILayout.TextField("Asset Search Root", assetSearchPath);

        EditorGUILayout.BeginHorizontal();
        autoMatchSprites = EditorGUILayout.Toggle("Auto-Match Sprites", autoMatchSprites);
        useNativeSize = EditorGUILayout.Toggle("Use Native Size", useNativeSize);
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginDisabledGroup(!autoMatchSprites);
        useFuzzyMatch = EditorGUILayout.Toggle("Fuzzy Sprite Match", useFuzzyMatch);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Build & Map Assets", GUILayout.Height(50)))
        {
            BuildUI();
        }
    }

    private void DrawSpriteToolsTab()
    {
        EditorGUILayout.HelpBox(
            "Generate a manifest of all sprite names in your project. Paste it into Axel's chat so he picks exact file names instead of guessing.",
            MessageType.Info);

        assetSearchPath = EditorGUILayout.TextField("Asset Search Root", assetSearchPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Sprite Manifest", GUILayout.Height(40)))
        {
            manifestPreview = GenerateSpriteManifest();
        }

        if (!string.IsNullOrEmpty(manifestPreview))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manifest Preview (also copied to clipboard & saved to Assets/):", EditorStyles.boldLabel);
            manifestScrollPos = EditorGUILayout.BeginScrollView(manifestScrollPos, GUILayout.Height(200));
            EditorGUILayout.TextArea(manifestPreview, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Copy to Clipboard Again"))
            {
                GUIUtility.systemCopyBuffer = manifestPreview;
            }
        }
    }

    private string GenerateSpriteManifest()
    {
        string searchPath = assetSearchPath.Replace("\\", "/").TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(searchPath)) searchPath = "Assets";

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { searchPath });
        var names = new List<string>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!names.Contains(name)) names.Add(name);
        }
        names.Sort();

        string manifest = "# Sprite Manifest — " + names.Count + " sprites\n# Paste this into Axel's chat so spriteName fields match exactly.\n\n" + string.Join("\n", names);

        string savePath = "Assets/sprite_manifest.txt";
        System.IO.File.WriteAllText(savePath, manifest);
        AssetDatabase.Refresh();
        GUIUtility.systemCopyBuffer = manifest;

        Debug.Log($"<b>Sprite Manifest:</b> {names.Count} sprites found. Saved to {savePath} and copied to clipboard.");
        return manifest;
    }

    private string ExtractJson(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"```json\s*(.*?)\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        match = System.Text.RegularExpressions.Regex.Match(text, @"```\s*(.*?)\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        int start = text.IndexOf('[');
        int end = text.LastIndexOf(']');
        if (start != -1 && end != -1 && end > start)
            return text.Substring(start, end - start + 1);

        return text;
    }

    private void BuildUI()
    {
        if (string.IsNullOrEmpty(jsonInput)) return;
        string formattedJson = jsonInput.Trim();
        try {
            if (formattedJson.StartsWith("```"))
                formattedJson = ExtractJson(formattedJson);

            formattedJson = System.Text.RegularExpressions.Regex.Replace(formattedJson, @",\s*([\]}])", "$1");

            if (formattedJson.StartsWith("["))
                formattedJson = "{\"elements\":" + formattedJson + "}";
            else if (!formattedJson.Contains("\"elements\""))
                formattedJson = "{\"elements\":[" + formattedJson + "]}";

            UILayoutData layout = JsonUtility.FromJson<UILayoutData>(formattedJson);
            if (layout == null || layout.elements == null) {
                Debug.LogError("Failed to parse JSON: Resulting layout or elements list is null.\nJSON: " + formattedJson);
                return;
            }

            GameObject root = Selection.activeGameObject;
            if (root == null || root.GetComponentInParent<Canvas>() == null) {
                Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null) {
                    GameObject canvasGo = new GameObject("UI_Root_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasGo.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    ConfigureCanvasScaler(canvas);
                }
                root = canvas.gameObject;
            }

            foreach (var element in layout.elements)
                if (element != null) CreateElement(element, root.transform);

            Debug.Log("<b>UI Build Complete!</b> Elements created: " + layout.elements.Count);
        } catch (System.Exception e) {
            Debug.LogError("UI Build Failed: " + e.Message + "\nFull JSON attempted:\n" + formattedJson);
        }
    }

    private void ConfigureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void CreateElement(UIElementData data, Transform parent)
    {
        GameObject go = new GameObject(data.name);
        Undo.RegisterCreatedObjectUndo(go, "Build UI Element");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(data.width, data.height);
        rt.anchoredPosition = new Vector2(data.posX, data.posY);
        string matchedSpriteName = "";
        string type = data.type.ToLower();
        switch (type) {
            case "panel": case "image": case "button":
                Image img = go.AddComponent<Image>();
                img.color = HexToColor(data.color);
                img.preserveAspect = true;
                if (autoMatchSprites) matchedSpriteName = TryAssignSprite(img, data.spriteName ?? data.name);
                if (type == "button") go.AddComponent<Button>();
                string inferredText = data.textValue;
                bool shouldInfer = data.name.ToLower().Contains("_txt") || matchedSpriteName.ToLower().Contains("_txt");
                if (string.IsNullOrEmpty(inferredText) && shouldInfer) {
                    string sourceName = matchedSpriteName.ToLower().Contains("_txt") ? matchedSpriteName : data.name;
                    inferredText = sourceName.Replace("Btn_", "").Replace("Img_", "").Replace("_txt", "").Replace("_", " ").ToUpper();
                }
                if (!string.IsNullOrEmpty(inferredText)) {
                    UIElementData btnText = new UIElementData {
                        name = "Label", type = "text", width = data.width * 0.9f, height = data.height * 0.9f,
                        textValue = inferredText, fontSize = data.fontSize > 0 ? data.fontSize : (int)(data.height * 0.35f), color = "#FFFFFF"
                    };
                    CreateElement(btnText, go.transform);
                }
                break;
            case "text":
                TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = data.textValue;
                txt.fontSize = data.fontSize > 0 ? data.fontSize : 36;
                txt.color = HexToColor(data.color);
                txt.alignment = TextAlignmentOptions.Center;
                txt.enableWordWrapping = false;
                txt.overflowMode = TextOverflowModes.Overflow;
                if (defaultFont != null) txt.font = defaultFont;
                // No explicit offset — stretch to fill parent so text is naturally centered
                if (data.posX == 0 && data.posY == 0) {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                break;
        }
        if (data.children != null)
            foreach (var child in data.children) CreateElement(child, go.transform);
    }

    private string TryAssignSprite(Image target, string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return "";
        string searchPath = assetSearchPath.Replace("\\", "/").TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(searchPath)) searchPath = "Assets";

        // Unity's FindAssets does substring matching — filter to exact filename match only
        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { searchPath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) { ApplySprite(target, s); return s.name; }
            }
        }

        if (useFuzzyMatch)
        {
            string[] allGuids = AssetDatabase.FindAssets("t:Sprite", new[] { searchPath });
            string bestPath = FindBestFuzzyMatch(spriteName, allGuids);
            if (!string.IsNullOrEmpty(bestPath))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(bestPath);
                if (s != null)
                {
                    ApplySprite(target, s);
                    Debug.Log($"[Fuzzy Match] '{spriteName}' → '{s.name}'");
                    return s.name;
                }
            }
        }

        return "";
    }

    private void ApplySprite(Image target, Sprite s)
    {
        target.sprite = s;
        target.color = Color.white;
        target.type = Image.Type.Simple;
        if (useNativeSize) target.SetNativeSize();
    }

    private string FindBestFuzzyMatch(string spriteName, string[] guids)
    {
        string[] queryTokens = spriteName.ToLower().Split(new char[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (queryTokens.Length == 0) return null;

        float bestScore = 0.35f;
        string bestPath = null;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            string[] nameTokens = name.Split(new char[] { '_', '-', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            int matches = 0;
            foreach (var qt in queryTokens)
                foreach (var nt in nameTokens)
                    if (nt == qt) { matches++; break; }

            float score = (float)matches / Mathf.Max(queryTokens.Length, nameTokens.Length);
            if (score > bestScore) { bestScore = score; bestPath = path; }
        }

        return bestPath;
    }

    private Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.white;
    }

    [System.Serializable] public class UILayoutData { public List<UIElementData> elements; }
    [System.Serializable] public class UIElementData {
        public string name; public string type; public string spriteName;
        public float posX; public float posY; public float width; public float height;
        public string color; public string textValue; public int fontSize;
        public List<UIElementData> children;
    }
}
