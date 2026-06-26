using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BlendShapeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMP_Text _statusText;
    [SerializeField] TMP_Text _blendShapeListText;
    [SerializeField] ScrollRect _scrollRect;
    [SerializeField] Button _recordButton;
    [SerializeField] Button _stopRecordButton;
    [SerializeField] Button _playButton;
    [SerializeField] Button _pauseButton;
    [SerializeField] Button _loadButton;
    [SerializeField] Slider _seekSlider;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] float _minBlendWeight = 0.1f;
    [SerializeField] int _maxBlendsDisplayed = 30;

    ARFaceManager _faceManager;
    FaceRecorder _recorder;
    FacePlayer _player;
    string _activeBlendText = "No face tracked.";
    string _statusLine = "";
    Dictionary<int, string> _blendNames;

    void Awake()
    {
        _faceManager = FindFirstObjectByType<ARFaceManager>();
        _recorder = GetComponent<FaceRecorder>();
        if (_recorder == null) _recorder = FindFirstObjectByType<FaceRecorder>();
        _player = GetComponent<FacePlayer>();
        if (_player == null) _player = FindFirstObjectByType<FacePlayer>();
    }

    void Start()
    {
        WireButtons();
        gameObject.SetActive(true);
    }

    void WireButtons()
    {
        if (_recordButton) _recordButton.onClick.AddListener(OnRecord);
        if (_stopRecordButton) _stopRecordButton.onClick.AddListener(OnStopRecord);
        if (_playButton) _playButton.onClick.AddListener(OnPlay);
        if (_pauseButton) _pauseButton.onClick.AddListener(OnPause);
        if (_loadButton) _loadButton.onClick.AddListener(OnLoad);
        if (_seekSlider) _seekSlider.onValueChanged.AddListener(v => _player?.Seek(v));
    }

    void OnRecord()
    {
        _recorder?.StartRecording();
    }

    void OnStopRecord()
    {
        _recorder?.StopRecording();
    }

    void OnPlay()
    {
        if (_player != null && _player.HasData)
        {
            _player.Play();
        }
        else
        {
            Debug.Log("[BlendShapeUI] No recording loaded. Record first or tap Load.");
        }
    }

    void OnPause()
    {
        _player?.Pause();
    }

    void OnLoad()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "face_recording.json");
        string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath, "*.json");
        if (files.Length > 0)
        {
            var sorted = new List<string>(files);
            sorted.Sort();
            sorted.Reverse();
            path = sorted[0];
        }

        _player?.LoadFromFile(path);
    }

    void Update()
    {
        UpdateStatus();
        UpdateBlendShapeList();
        UpdateSeekSlider();
    }

    void UpdateStatus()
    {
        var sb = new StringBuilder();

        if (_faceManager != null)
        {
            sb.AppendLine($"Faces tracked: {_faceManager.trackables.count}");
            foreach (var face in _faceManager.trackables)
            {
                sb.AppendLine($"  State: {face.trackingState}");
                sb.AppendLine($"  Mesh: {VertCount(face.vertices)}v / {TriCount(face.indices)}t");
                sb.AppendLine($"  Eyes: {(face.leftEye != null ? "L+R" : "none")}");
            }
        }
        else
        {
            sb.AppendLine("No ARFaceManager found.");
        }

        if (_recorder != null)
        {
            sb.AppendLine(_recorder.IsRecording
                ? $"RECORDING | Frames: {_recorder.FrameCount} | T: {_recorder.RecordingTime:F1}s"
                : "Idle");
        }

        if (_player != null && _player.HasData)
        {
            string state = _player.IsPlaying ? (_player.IsPaused ? "PAUSED" : "PLAYING") : "STOPPED";
            sb.AppendLine($"Playback: {state} | {_player.CurrentTime:F1}s / {_player.TotalTime:F1}s");
        }

        _statusLine = sb.ToString();
        if (_statusText) _statusText.text = _statusLine;
    }

    void UpdateBlendShapeList()
    {
        if (_faceManager == null || _faceManager.trackables.count == 0)
        {
            if (_blendShapeListText) _blendShapeListText.text = _activeBlendText;
            return;
        }

        var sb = new StringBuilder();
        var ss = _faceManager.subsystem;

        foreach (var face in _faceManager.trackables)
        {
            if (face.trackingState == TrackingState.None) continue;

            try
            {
                var result = ss.TryGetBlendShapes(face.trackableId, Allocator.Temp);
                if (!result.status.IsSuccess() || !result.value.IsCreated)
                {
                    sb.AppendLine("No blend shapes (need TrueDepth device).");
                    continue;
                }

                var names = GetBlendNames();
                int displayed = 0;
                foreach (var bs in result.value)
                {
                    if (bs.weight < _minBlendWeight) continue;
                    if (displayed++ >= _maxBlendsDisplayed) break;

                    string n = names.TryGetValue(bs.blendShapeId, out var nm) ? nm : $"id_{bs.blendShapeId}";
                    string bar = MakeBar(bs.weight);
                    sb.AppendLine($"{n,-28} {bar} {bs.weight:F2}");
                }
                result.value.Dispose();
            }
            catch (Exception e)
            {
                sb.AppendLine($"Error: {e.Message}");
            }
        }

        _activeBlendText = sb.Length > 0 ? sb.ToString() : "No active blendshapes above threshold.";

        if (_blendShapeListText)
        {
            _blendShapeListText.text = _activeBlendText;
            Canvas.ForceUpdateCanvases();
            if (_scrollRect) _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void UpdateSeekSlider()
    {
        if (_seekSlider && _player != null && _player.HasData)
            _seekSlider.SetValueWithoutNotify(_player.Progress);
    }

    static string MakeBar(float val)
    {
        int blocks = Mathf.RoundToInt(val * 20f);
        return "[" + new string('|', blocks) + new string('.', 20 - blocks) + "]";
    }

    static int VertCount(NativeArray<Vector3> arr) => arr.IsCreated ? arr.Length : 0;
    static int TriCount(NativeArray<int> arr) => arr.IsCreated ? arr.Length / 3 : 0;

    Dictionary<int, string> GetBlendNames()
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
}