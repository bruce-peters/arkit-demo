using System;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    [SerializeField] MeshFilter _playbackMeshFilter;
    [SerializeField] Material _playbackMaterial;
    [SerializeField] bool _createDisplayObjects = true;

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public float CurrentTime { get; private set; }
    public float TotalTime { get; private set; }
    public float Progress => TotalTime > 0f ? CurrentTime / TotalTime : 0f;
    public bool HasData => _recordingData != null && _recordingData.frames.Count > 0;

    FaceRecordingData _recordingData;
    Mesh _playbackMesh;
    GameObject _displayRoot;
    LineRenderer _eyeLineLeft;
    LineRenderer _eyeLineRight;

    public void LoadFromFile(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"[FacePlayer] File not found: {path}");
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        LoadFromJson(json);
    }

    public void LoadFromJson(string json)
    {
        try
        {
            _recordingData = JsonUtility.FromJson<FaceRecordingData>(json);
            TotalTime = _recordingData.frames.Count > 0
                ? _recordingData.frames[^1].time
                : 0f;
            Debug.Log($"[FacePlayer] Loaded {_recordingData.frameCount} frames, duration {TotalTime:F2}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FacePlayer] Failed to parse: {e.Message}");
            _recordingData = null;
        }
    }

    public void Play()
    {
        if (!HasData) return;
        IsPlaying = true;
        IsPaused = false;
        EnsureDisplayObjects();
    }

    public void Pause()
    {
        IsPaused = !IsPaused;
    }

    public void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        CurrentTime = 0f;
    }

    public void Seek(float normalizedTime)
    {
        CurrentTime = Mathf.Clamp01(normalizedTime) * TotalTime;
    }

    void Update()
    {
        if (!IsPlaying || IsPaused || !HasData) return;

        CurrentTime += Time.deltaTime;
        if (CurrentTime >= TotalTime)
        {
            CurrentTime = 0f;
        }

        var frame = GetFrameAtTime(CurrentTime);
        if (frame != null)
            ApplyFrame(frame);
    }

    FaceDataFrame GetFrameAtTime(float time)
    {
        var frames = _recordingData.frames;
        int lo = 0, hi = frames.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (frames[mid].time < time)
                lo = mid + 1;
            else
                hi = mid - 1;
        }
        int idx = Mathf.Clamp(lo, 0, frames.Count - 1);
        return frames[idx];
    }

    void ApplyFrame(FaceDataFrame frame)
    {
        if (_playbackMesh == null) return;

        if (!string.IsNullOrEmpty(frame.verticesB64) && frame.vertexCount > 0)
        {
            var verts = DecodeVector3Array(frame.verticesB64, frame.vertexCount);
            _playbackMesh.SetVertices(verts);

            if (!string.IsNullOrEmpty(frame.normalsB64))
            {
                var norms = DecodeVector3Array(frame.normalsB64, frame.vertexCount);
                _playbackMesh.SetNormals(norms);
            }

            if (!string.IsNullOrEmpty(frame.indicesB64))
            {
                var indices = DecodeIntArray(frame.indicesB64);
                _playbackMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            }
        }

        if (!string.IsNullOrEmpty(frame.verticesB64) && frame.vertexCount > 0)
        {
            _displayRoot.transform.position = frame.position.ToVector3();
            _displayRoot.transform.eulerAngles = frame.rotation.ToVector3();
        }

        UpdateEyeLine(_eyeLineLeft, frame.leftEye);
        UpdateEyeLine(_eyeLineRight, frame.rightEye);
    }

    void UpdateEyeLine(LineRenderer lr, EyeRecord eye)
    {
        if (lr == null) return;
        lr.SetPosition(0, _displayRoot.transform.position + eye.position.ToVector3());
        lr.SetPosition(1, _displayRoot.transform.position + eye.position.ToVector3() +
                         eye.rotation.ToVector3().normalized * 0.03f);
    }

    void EnsureDisplayObjects()
    {
        if (!_createDisplayObjects) return;

        if (_displayRoot == null)
        {
            _displayRoot = new GameObject("FacePlayback");
            _displayRoot.transform.SetParent(transform);
        }

        if (_playbackMeshFilter == null)
        {
            var go = new GameObject("Mesh");
            go.transform.SetParent(_displayRoot.transform, false);
            _playbackMeshFilter = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            if (_playbackMaterial != null) mr.material = _playbackMaterial;
        }

        if (_playbackMesh == null)
            _playbackMesh = new Mesh { name = "PlaybackFaceMesh" };

        _playbackMeshFilter.mesh = _playbackMesh;

        if (_eyeLineLeft == null)
            _eyeLineLeft = CreateEyeLine("EyeLineLeft", Color.red);
        if (_eyeLineRight == null)
            _eyeLineRight = CreateEyeLine("EyeLineRight", Color.blue);
    }

    LineRenderer CreateEyeLine(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_displayRoot.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = 0.002f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        return lr;
    }

    static List<Vector3> DecodeVector3Array(string b64, int count)
    {
        var bytes = Convert.FromBase64String(b64);
        var floats = BytesToFloats(bytes);
        var list = new List<Vector3>(count);
        for (int i = 0; i < count && (i * 3 + 2) < floats.Length; i++)
            list.Add(new Vector3(floats[i * 3], floats[i * 3 + 1], floats[i * 3 + 2]));
        return list;
    }

    static int[] DecodeIntArray(string b64)
    {
        var bytes = Convert.FromBase64String(b64);
        var result = new int[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        return result;
    }

    static float[] BytesToFloats(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}