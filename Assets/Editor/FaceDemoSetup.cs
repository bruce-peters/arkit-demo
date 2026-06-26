using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public static class FaceDemoSetup
{
    const string PrefabPath = "Assets/Prefabs/FaceMeshPrefab.prefab";
    const string MaterialPath = "Assets/Prefabs/FaceWireframe.mat";
    const string PrefabDir = "Assets/Prefabs";

    [MenuItem("ARKit Demo/Setup Face Tracking Demo")]
    public static void SetupAll()
    {
        EnsureDirectories();
        Material mat = CreateOrGetMaterial();
        GameObject facePrefab = CreateOrGetFacePrefab(mat);
        CreateFaceDataManager(facePrefab);
        CreateDebugCanvas();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FaceDemoSetup] Done. Face tracking demo is ready.\n" +
                  "  1. Assign the Face Mesh Prefab to ARFaceManager if not already assigned.\n" +
                  "  2. Press Play or Build to device.");
    }

    [MenuItem("ARKit Demo/Setup Face Tracking Demo", validate = true)]
    public static bool ValidateSetup()
    {
        return !EditorApplication.isPlaying;
    }

    static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
    }

    static Material CreateOrGetMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        mat.name = "FaceWireframe";
        mat.color = new Color(1f, 1f, 1f, 0.4f);
        mat.SetFloat("_Surface", 1); // transparent
        mat.SetFloat("_Blend", 0);
        mat.renderQueue = 3000;

        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    static GameObject CreateOrGetFacePrefab(Material mat)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        var go = new GameObject("FaceMesh");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;
        go.AddComponent<ARFaceMeshVisualizer>();
        go.AddComponent<FaceMeshVisualizerExtras>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        return prefab;
    }

    static void CreateFaceDataManager(GameObject facePrefab)
    {
        var existing = GameObject.Find("FaceDataManager");
        if (existing != null)
        {
            Debug.Log("[FaceDemoSetup] FaceDataManager already exists. Updating references.");
            ConfigureDataManager(existing, facePrefab);
            return;
        }

        var go = new GameObject("FaceDataManager");

        var recorder = go.AddComponent<FaceRecorder>();
        var player = go.AddComponent<FacePlayer>();
        var ui = go.AddComponent<BlendShapeUI>();

        ConfigureFaceManager(facePrefab);

        Selection.activeGameObject = go;
        Debug.Log("[FaceDemoSetup] Created FaceDataManager with FaceRecorder + FacePlayer + BlendShapeUI.");
    }

    static void ConfigureDataManager(GameObject go, GameObject facePrefab)
    {
        ConfigureFaceManager(facePrefab);
    }

    static void ConfigureFaceManager(GameObject facePrefab)
    {
        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogWarning("[FaceDemoSetup] No XR Origin found. Cannot configure ARFaceManager.");
            return;
        }

        var faceManager = origin.GetComponent<ARFaceManager>();
        if (faceManager == null)
            faceManager = origin.gameObject.AddComponent<ARFaceManager>();

        if (facePrefab != null)
        {
            var serialized = new SerializedObject(faceManager);
            var prefabProp = serialized.FindProperty("m_FacePrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = facePrefab;
                serialized.ApplyModifiedProperties();
            }

            var maxProp = serialized.FindProperty("m_MaximumFaceCount");
            if (maxProp != null)
            {
                maxProp.intValue = 1;
                serialized.ApplyModifiedProperties();
            }
        }

        faceManager.enabled = true;
    }

    static void CreateDebugCanvas()
    {
        var existing = GameObject.Find("FaceDebugCanvas");
        if (existing != null)
        {
            WireCanvas(existing.GetComponent<Canvas>());
            return;
        }

        var canvasGO = new GameObject("FaceDebugCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = CreatePanel(canvasGO.transform, "Panel");
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.01f, 0.01f);
        panelRect.anchorMax = new Vector2(0.45f, 0.99f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        var scrollView = CreateScrollView(panel.transform, "ScrollView");
        var contentTMP = CreateTMPText(scrollView.transform, "BlendShapeText", 16, anchorTop: true);

        var controlsPanel = CreatePanel(canvasGO.transform, "ControlsPanel");
        var controlsRect = controlsPanel.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.50f, 0.01f);
        controlsRect.anchorMax = new Vector2(0.99f, 0.99f);
        var controlsBg = controlsPanel.AddComponent<Image>();
        controlsBg.color = new Color(0, 0, 0, 0.7f);

        var controlsLayout = controlsPanel.AddComponent<VerticalLayoutGroup>();
        controlsLayout.padding = new RectOffset(10, 10, 10, 10);
        controlsLayout.spacing = 10;

        var statusTMP = CreateTMPText(controlsPanel.transform, "StatusText", 18, anchorTop: true);
        statusTMP.gameObject.AddComponent<LayoutElement>();

        CreateButton(controlsPanel.transform, "BtnRecord", "Start Record", 32);
        CreateButton(controlsPanel.transform, "BtnStopRecord", "Stop Record", 32);
        CreateButton(controlsPanel.transform, "BtnPlay", "Play", 32);
        CreateButton(controlsPanel.transform, "BtnPause", "Pause", 32);
        CreateButton(controlsPanel.transform, "BtnLoad", "Load Recording", 32);

        var seekGO = new GameObject("SeekSlider");
        seekGO.transform.SetParent(controlsPanel.transform, false);
        var slider = seekGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        var sliderBG = seekGO.AddComponent<Image>();
        sliderBG.color = new Color(0.3f, 0.3f, 0.3f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(seekGO.transform, false);
        var fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-20, 0);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillChildRect = fill.AddComponent<RectTransform>();
        fillChildRect.sizeDelta = new Vector2(10, 0);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.white;
        slider.fillRect = fillChildRect;
        slider.targetGraphic = fillImg;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(seekGO.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero; handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);
        slider.handleRect = handleAreaRect;

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        WireCanvas(canvas);
    }

    static void WireCanvas(Canvas canvas)
    {
        var ui = Object.FindFirstObjectByType<BlendShapeUI>();
        if (ui == null)
        {
            Debug.LogWarning("[FaceDemoSetup] No BlendShapeUI in scene. Create FaceDataManager first.");
            return;
        }

        var so = new SerializedObject(ui);

        var statusText = canvas.transform.Find("ControlsPanel/StatusText");
        if (statusText) so.FindProperty("_statusText").objectReferenceValue = statusText.GetComponent<TMP_Text>();

        var bsText = RecursiveFind(canvas.transform, "BlendShapeText");
        if (bsText) so.FindProperty("_blendShapeListText").objectReferenceValue = bsText.GetComponent<TMP_Text>();

        var scroll = canvas.transform.Find("Panel/ScrollView")?.GetComponent<ScrollRect>();
        if (scroll) so.FindProperty("_scrollRect").objectReferenceValue = scroll;

        var recordBtn = canvas.transform.Find("ControlsPanel/BtnRecord")?.GetComponent<Button>();
        if (recordBtn) so.FindProperty("_recordButton").objectReferenceValue = recordBtn;

        var stopRecordBtn = canvas.transform.Find("ControlsPanel/BtnStopRecord")?.GetComponent<Button>();
        if (stopRecordBtn) so.FindProperty("_stopRecordButton").objectReferenceValue = stopRecordBtn;

        var playBtn = canvas.transform.Find("ControlsPanel/BtnPlay")?.GetComponent<Button>();
        if (playBtn) so.FindProperty("_playButton").objectReferenceValue = playBtn;

        var pauseBtn = canvas.transform.Find("ControlsPanel/BtnPause")?.GetComponent<Button>();
        if (pauseBtn) so.FindProperty("_pauseButton").objectReferenceValue = pauseBtn;

        var loadBtn = canvas.transform.Find("ControlsPanel/BtnLoad")?.GetComponent<Button>();
        if (loadBtn) so.FindProperty("_loadButton").objectReferenceValue = loadBtn;

        var slider = canvas.transform.Find("ControlsPanel/SeekSlider")?.GetComponent<Slider>();
        if (slider) so.FindProperty("_seekSlider").objectReferenceValue = slider;

        so.ApplyModifiedProperties();
    }

    static Transform RecursiveFind(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = RecursiveFind(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = Vector2.zero;
        return go;
    }

    static GameObject CreateScrollView(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.sizeDelta = new Vector2(-20, -20);
        rect.anchoredPosition = new Vector2(10, -10);

        go.AddComponent<ScrollRect>();
        var mask = go.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

        var content = new GameObject("Content");
        content.transform.SetParent(go.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        go.GetComponent<ScrollRect>().content = contentRect;

        return content;
    }

    static TMP_Text CreateTMPText(Transform parent, string name, int fontSize, bool anchorTop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 0);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.text = anchorTop ? "Awaiting face data..." : "Ready";
        tmp.alignment = anchorTop
            ? TMPro.TextAlignmentOptions.TopLeft
            : TMPro.TextAlignmentOptions.Center;

        return tmp;
    }

    static Button CreateButton(Transform parent, string name, string label, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.55f, 0.9f);

        var btn = go.AddComponent<Button>();
        img.color = btn.colors.normalColor;
        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.55f, 0.9f);
        colors.highlightedColor = new Color(0.35f, 0.65f, 1f);
        colors.pressedColor = new Color(0.15f, 0.45f, 0.8f);
        btn.colors = colors;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        go.AddComponent<LayoutElement>();

        return btn;
    }
}