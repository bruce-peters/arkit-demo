// FaceDataProbe.cs
//
// Quick mockup to see exactly what face data ARKit gives you through AR Foundation.
// Put this on one GameObject in the AR template scene and Build & Run to a TrueDepth iPhone/iPad.
// It auto-adds an ARFaceManager, flips the camera to the front (selfie) camera, and every frame
// dumps, for each tracked face:
//   - tracking state + pose
//   - face mesh size (verts / triangles / uvs)
//   - left eye, right eye, and gaze fixation point (if the device reports them)
//   - all ARKit blendshape weights (jawOpen, eyeBlinkLeft, ... 0..1), named at runtime
//
// Verified against AR Foundation / Apple ARKit XR Plug-in 6.4.
// Requirements: enable Face Tracking in Project Settings > XR Plug-in Management > Apple ARKit,
// and install the "Apple ARKit Face Tracking" package. See FACE_TRACKING.md.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public class FaceDataProbe : MonoBehaviour
{
    [Tooltip("Force the front (selfie) camera at start. ARKit face tracking needs it.")]
    public bool forceFrontCamera = true;

    [Tooltip("Hide blendshapes below this weight to cut noise. 0 shows all ~52.")]
    [Range(0f, 1f)] public float minBlendWeight = 0f;

    [Tooltip("On-screen font size. 0 = auto-scale to screen height.")]
    public int fontSize = 0;

    ARFaceManager _faces;
    ARCameraManager _cam;
    string _report = "Waiting for a face... point the front camera at yourself.";
    Vector2 _scroll;
    GUIStyle _style;

    static Dictionary<int, string> _blendNames;

    void Awake()
    {
        _faces = FindFirstObjectByType<ARFaceManager>();
        if (_faces == null)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
            {
                _faces = origin.gameObject.AddComponent<ARFaceManager>();
                Debug.Log("[FaceDataProbe] Added ARFaceManager to XR Origin.");
            }
            else
            {
                Debug.LogError("[FaceDataProbe] No XR Origin in scene. Open the AR template scene, " +
                               "or add GameObject > XR > XR Origin (Mobile AR).");
            }
        }
        _cam = FindFirstObjectByType<ARCameraManager>();
    }

    void Start()
    {
        if (forceFrontCamera && _cam != null)
            _cam.requestedFacingDirection = CameraFacingDirection.User;
    }

    void Update()
    {
        if (_faces == null) return;
        try { _report = BuildReport(); }
        catch (Exception e) { _report = "Probe error: " + e; }
    }

    string BuildReport()
    {
        var sb = new StringBuilder();
        var ss = _faces.subsystem;

        sb.AppendLine("=== ARKit face data (AR Foundation) ===");
        sb.AppendLine($"Camera facing: requested {(_cam ? _cam.requestedFacingDirection.ToString() : "?")}, " +
                      $"current {(_cam ? _cam.currentFacingDirection.ToString() : "?")}");
        sb.AppendLine($"Faces tracked: {_faces.trackables.count}" +
                      (ss != null ? $"   (provider supports up to {SafeSupported(ss)})" : "   (no provider - run on device)"));
        sb.AppendLine();

        if (_faces.trackables.count == 0)
        {
            sb.AppendLine("No face yet. On a non-TrueDepth device or in the Editor you'll see nothing here.");
            return sb.ToString();
        }

        foreach (var face in _faces.trackables)
        {
            sb.AppendLine($"FACE {face.trackableId}");
            sb.AppendLine($"  trackingState: {face.trackingState}");
            sb.AppendLine($"  pose: pos {V(face.transform.position)}  rot {V(face.transform.eulerAngles)}");

            int verts = face.vertices.IsCreated ? face.vertices.Length : 0;
            int tris  = face.indices.IsCreated ? face.indices.Length / 3 : 0;
            int uvs   = face.uvs.IsCreated ? face.uvs.Length : 0;
            sb.AppendLine($"  mesh: {verts} verts, {tris} tris, {uvs} uvs");

            sb.AppendLine($"  leftEye:  {Eye(face.leftEye)}");
            sb.AppendLine($"  rightEye: {Eye(face.rightEye)}");
            sb.AppendLine($"  gaze fixation: {(face.fixationPoint ? V(face.fixationPoint.localPosition) : "n/a")}");

            AppendBlendShapes(sb, ss, face.trackableId);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    void AppendBlendShapes(StringBuilder sb, XRFaceSubsystem ss, TrackableId id)
    {
        if (ss == null) { sb.AppendLine("  blendshapes: n/a (no subsystem)"); return; }

        try
        {
            var result = ss.TryGetBlendShapes(id, Allocator.Temp);
            if (!result.status.IsSuccess() || !result.value.IsCreated)
            {
                sb.AppendLine("  blendshapes: none (need a TrueDepth device with face tracking enabled)");
                return;
            }

            var names = BlendNames();
            sb.AppendLine($"  blendshapes: {result.value.Length}");
            foreach (var bs in result.value)
            {
                if (bs.weight < minBlendWeight) continue;
                string n = names.TryGetValue(bs.blendShapeId, out var nm) ? nm : $"id {bs.blendShapeId}";
                sb.AppendLine($"    {n} = {bs.weight:F2}");
            }
            result.value.Dispose();
        }
        catch (Exception e)
        {
            sb.AppendLine("  blendshapes: error " + e.Message);
        }
    }

    static string SafeSupported(XRFaceSubsystem ss)
    {
        try { return ss.supportedFaceCount.ToString(); } catch { return "?"; }
    }

    static string Eye(Transform t) =>
        t ? $"pos {V(t.localPosition)}  rot {V(t.localEulerAngles)}" : "n/a";

    static string V(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

    // ARKit reports blendshapes by integer id. Build id -> friendly name from the ARKit enum at
    // runtime (via reflection, so this file needs no compile-time ARKit reference). Falls back to
    // "id N" if the ARKit Face Tracking package isn't present.
    static Dictionary<int, string> BlendNames()
    {
        if (_blendNames != null) return _blendNames;
        _blendNames = new Dictionary<int, string>();

        Type enumType = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            enumType = asm.GetType("UnityEngine.XR.ARKit.ARKitBlendShapeLocation", false);
            if (enumType != null) break;
        }

        if (enumType != null && enumType.IsEnum)
            foreach (var val in Enum.GetValues(enumType))
                _blendNames[Convert.ToInt32(val)] = val.ToString();

        return _blendNames;
    }

    void OnGUI()
    {
        int fs = fontSize > 0 ? fontSize : Mathf.Max(14, Screen.height / 50);
        if (_style == null || _style.fontSize != fs)
            _style = new GUIStyle(GUI.skin.label) { fontSize = fs, wordWrap = true };

        GUILayout.BeginArea(new Rect(8, 8, Screen.width - 16, Screen.height - 16), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Label(_report, _style);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}