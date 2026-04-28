using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class UIAutoBuilder : EditorWindow
{
    private string jsonInput = "";
    private Vector2 scrollPos;
    
    // Settings
    private TMP_FontAsset defaultFont;
    private string assetSearchPath = "Assets";
    private bool autoMatchSprites = true;
    private bool useNativeSize = true;

    // AI Vision Settings
    private bool showVisionTab = false;
    private Texture2D mockupTexture;
    private string apiKey = "";
    private bool isAnalyzing = false;

    [MenuItem("Tools/Universal UI/Auto-Builder Pro")]
    public static void ShowWindow()
    {
        GetWindow<UIAutoBuilder>("UI Auto-Builder Pro");
    }

    private void OnEnable()
    {
        // Load API key from local storage
        apiKey = EditorPrefs.GetString("UIAutoBuilder_ApiKey", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Universal UI Auto-Builder Pro", EditorStyles.boldLabel);
        
        showVisionTab = GUILayout.Toolbar(showVisionTab ? 1 : 0, new string[] { "Manual JSON", "AI Vision (BETA)" }) == 1;

        EditorGUILayout.Space();
        
        if (showVisionTab)
        {
            DrawVisionTab();
        }
        else
        {
            DrawManualTab();
        }
    }

    private void DrawManualTab()
    {
        defaultFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Default TMP Font", defaultFont, typeof(TMP_FontAsset), false);
        assetSearchPath = EditorGUILayout.TextField("Asset Search Root", assetSearchPath);
        
        EditorGUILayout.BeginHorizontal();
        autoMatchSprites = EditorGUILayout.Toggle("Auto-Match Sprites", autoMatchSprites);
        useNativeSize = EditorGUILayout.Toggle("Use Native Size", useNativeSize);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Build & Map Assets", GUILayout.Height(50)))
        {
            BuildUI();
        }
    }

    private void DrawVisionTab()
    {
        apiKey = EditorGUILayout.PasswordField("Gemini API Key", apiKey);
        if (GUI.changed) EditorPrefs.SetString("UIAutoBuilder_ApiKey", apiKey);

        mockupTexture = (Texture2D)EditorGUILayout.ObjectField("Mockup Screenshot", mockupTexture, typeof(Texture2D), false);
        
        EditorGUILayout.HelpBox("This will send the image to Gemini Vision to generate the JSON automatically. Requires a free API key from Google AI Studio.", MessageType.Info);

        EditorGUI.BeginDisabledGroup(mockupTexture == null || string.IsNullOrEmpty(apiKey) || isAnalyzing);
        if (GUILayout.Button(isAnalyzing ? "Analyzing Mockup..." : "Analyze & Build UI", GUILayout.Height(50)))
        {
            EditorCoroutineRunner.StartCoroutine(AnalyzeWithAI());
        }
        EditorGUI.EndDisabledGroup();
    }

    private IEnumerator AnalyzeWithAI()
    {
        isAnalyzing = true;
        
        byte[] imageBytes = mockupTexture.EncodeToPNG();
        string base64Image = System.Convert.ToBase64String(imageBytes);

        // Prompt for the AI
        string prompt = "Analyze this mobile UI mockup. Return a JSON list of UI elements. " +
                        "Each element must have: name, type (image, button, text, panel), posX, posY, width, height, color (hex). " +
                        "Use a 1080x1920 coordinate system (center is 0,0). " +
                        "If it's a button with text, include 'textValue'. If a sprite should be found, make 'name' match the likely filename. " +
                        "Return ONLY the raw JSON array.";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        
        string payload = "{\"contents\":[{\"parts\":[{\"text\":\"" + prompt + "\"},{\"inline_data\":{\"mime_type\":\"image/png\",\"data\":\"" + base64Image + "\"}}]}]}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the response (Simplified for Gemini's structure)
                string responseText = request.downloadHandler.text;
                // Note: Real parsing would extract text from contents[0].parts[0].text
                // For this demo, we'll extract the JSON block using Regex
                var match = System.Text.RegularExpressions.Regex.Match(responseText, @"\[\s*\{.*\}\s*\]", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (match.Success)
                {
                    jsonInput = match.Value;
                    showVisionTab = false; // Switch to manual to show the result
                    BuildUI();
                }
                else
                {
                    Debug.LogError("Could not parse JSON from AI response: " + responseText);
                }
            }
            else
            {
                Debug.LogError("AI Analysis Failed: " + request.error);
            }
        }

        isAnalyzing = false;
    }

    // --- REST OF THE BUILD LOGIC REMAINS THE SAME ---
    private void BuildUI() { /* Same as previous version */ 
        if (string.IsNullOrEmpty(jsonInput)) return;
        try {
            string formattedJson = jsonInput.Trim();
            if (!formattedJson.StartsWith("{\"elements\":")) formattedJson = "{\"elements\":" + formattedJson + "}";
            UILayoutData layout = JsonUtility.FromJson<UILayoutData>(formattedJson);
            GameObject root = Selection.activeGameObject;
            if (root == null || root.GetComponentInParent<Canvas>() == null) {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null) {
                    GameObject canvasGo = new GameObject("UI_Root_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasGo.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                root = canvas.gameObject;
            }
            foreach (var element in layout.elements) { CreateElement(element, root.transform); }
            Debug.Log("<b>UI Build Complete!</b>");
        } catch (System.Exception e) { Debug.LogError("UI Build Failed: " + e.Message); }
    }

    private void CreateElement(UIElementData data, Transform parent) {
        GameObject go = new GameObject(data.name);
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
                if (defaultFont != null) txt.font = defaultFont;
                break;
        }
        if (data.children != null) { foreach (var child in data.children) { CreateElement(child, go.transform); } }
    }

    private string TryAssignSprite(Image target, string spriteName) {
        if (string.IsNullOrEmpty(spriteName)) return "";
        string searchPath = assetSearchPath.Replace("\\", "/").TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(searchPath)) searchPath = "Assets";
        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { searchPath });
        if (guids.Length > 0) {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) {
                target.sprite = s;
                target.color = Color.white;
                target.type = Image.Type.Simple;
                if (useNativeSize) target.SetNativeSize();
                return s.name;
            }
        }
        return "";
    }

    private Color HexToColor(string hex) {
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

    // Helper class for Editor Coroutines
    public static class EditorCoroutineRunner {
        public static void StartCoroutine(IEnumerator coroutine) {
            EditorApplication.update += () => {
                if (!coroutine.MoveNext()) EditorApplication.update -= null;
            };
        }
    }
}
