using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FaceRecorder : MonoBehaviour
{
    [SerializeField] ARFaceManager _faceManager;
    [SerializeField] int _targetFrameRate = 60;
    [SerializeField] [Range(0f, 1f)] float _minBlendWeight = 0f;

    public bool IsRecording { get; private set; }
    public int FrameCount => _recordingData?.frameCount ?? 0;
    public float RecordingTime { get; private set; }

    public event Action<FaceRecordingData> OnRecordingSaved;

    FaceRecordingData _recordingData;
    float _startTime;
    float _lastFrameTime;
    Dictionary<int, string> _blendNames;
    bool _wantsRecord;
    bool _wantsStop;

    void Awake()
    {
        if (_faceManager == null)
            _faceManager = FindFirstObjectByType<ARFaceManager>();
    }

    void Update()
    {
        if (!IsRecording) return;

        float now = Time.time;
        float interval = 1f / Mathf.Max(1, _targetFrameRate);
        if (now - _lastFrameTime < interval) return;
        _lastFrameTime = now;

        CaptureFrame(now - _startTime);
    }

    public void StartRecording()
    {
        if (IsRecording) return;
        if (_faceManager == null)
        {
            Debug.LogError("[FaceRecorder] No ARFaceManager assigned.");
            return;
        }

        _recordingData = new FaceRecordingData
        {
            recordedAt = DateTime.UtcNow.ToString("O"),
            estimatedFrameRate = _targetFrameRate
        };
        _startTime = Time.time;
        _lastFrameTime = 0f;
        IsRecording = true;
        Debug.Log("[FaceRecorder] Recording started.");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        IsRecording = false;
        Debug.Log($"[FaceRecorder] Recording stopped. Frames: {_recordingData.frameCount}");

        string path = System.IO.Path.Combine(Application.persistentDataPath, "face_recording.json");
        string json = JsonUtility.ToJson(_recordingData, true);
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[FaceRecorder] Saved to {path} ({json.Length} chars)");

        OnRecordingSaved?.Invoke(_recordingData);
    }

    void CaptureFrame(float time)
    {
        var frame = new FaceDataFrame { time = time };

        foreach (var face in _faceManager.trackables)
        {
            if (face.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.None)
                continue;

            frame.trackingState = (int)face.trackingState;
            frame.position = new SerializableVector3(face.transform.position);
            frame.rotation = new SerializableVector3(face.transform.eulerAngles);

            frame.leftEye = CaptureEye(face.leftEye);
            frame.rightEye = CaptureEye(face.rightEye);
            frame.gazeFixation = face.fixationPoint != null
                ? new EyeRecord
                {
                    position = new SerializableVector3(face.fixationPoint.localPosition),
                    rotation = new SerializableVector3(face.fixationPoint.localEulerAngles)
                }
                : new EyeRecord();

            CaptureBlendShapes(frame, face.trackableId);
            CaptureMesh(frame, face);

            break; // only one face
        }

        _recordingData.frames.Add(frame);
        _recordingData.frameCount = _recordingData.frames.Count;
        RecordingTime = time;
    }

    void CaptureBlendShapes(FaceDataFrame frame, UnityEngine.XR.ARSubsystems.TrackableId id)
    {
        var ss = _faceManager.subsystem;
        if (ss == null) return;

        try
        {
            var result = ss.TryGetBlendShapes(id, Allocator.Temp);
            if (!result.status.IsSuccess() || !result.value.IsCreated) return;

            var names = GetBlendNames();
            var list = new List<BlendShapeRecord>();
            foreach (var bs in result.value)
            {
                if (bs.weight < _minBlendWeight) continue;
                list.Add(new BlendShapeRecord
                {
                    id = bs.blendShapeId,
                    name = names.TryGetValue(bs.blendShapeId, out var n) ? n : $"id_{bs.blendShapeId}",
                    weight = bs.weight
                });
            }
            frame.blendShapes = list.ToArray();
            result.value.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FaceRecorder] BlendShape error: {e.Message}");
        }
    }

    void CaptureMesh(FaceDataFrame frame, ARFace face)
    {
        if (face.vertices.IsCreated && face.vertices.Length > 0)
        {
            frame.vertexCount = face.vertices.Length;
            frame.verticesB64 = EncodeFloatArray(face.vertices);
            frame.normalsB64 = face.normals.IsCreated ? EncodeFloatArray(face.normals) : null;
            frame.indicesB64 = face.indices.IsCreated ? EncodeIntArray(face.indices) : null;
            frame.uvsB64 = face.uvs.IsCreated ? EncodeFloatArray(face.uvs) : null;
        }
    }

    static EyeRecord CaptureEye(Transform t)
    {
        return t != null
            ? new EyeRecord
            {
                position = new SerializableVector3(t.localPosition),
                rotation = new SerializableVector3(t.localEulerAngles)
            }
            : new EyeRecord();
    }

    static string EncodeFloatArray(NativeArray<Vector3> arr)
    {
        var floats = new float[arr.Length * 3];
        for (int i = 0; i < arr.Length; i++)
        {
            floats[i * 3] = arr[i].x;
            floats[i * 3 + 1] = arr[i].y;
            floats[i * 3 + 2] = arr[i].z;
        }
        return Convert.ToBase64String(FloatsToBytes(floats));
    }

    static string EncodeFloatArray(NativeArray<Vector2> arr)
    {
        var floats = new float[arr.Length * 2];
        for (int i = 0; i < arr.Length; i++)
        {
            floats[i * 2] = arr[i].x;
            floats[i * 2 + 1] = arr[i].y;
        }
        return Convert.ToBase64String(FloatsToBytes(floats));
    }

    static string EncodeIntArray(NativeArray<int> arr)
    {
        var ints = new int[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            ints[i] = arr[i];

        var bytes = new byte[ints.Length * sizeof(int)];
        Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    static byte[] FloatsToBytes(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

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