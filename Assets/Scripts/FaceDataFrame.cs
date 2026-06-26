using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BlendShapeRecord
{
    public int id;
    public string name;
    public float weight;
}

[Serializable]
public struct EyeRecord
{
    public SerializableVector3 position;
    public SerializableVector3 rotation;
}

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x; y = v.y; z = v.z;
    }

    public Vector3 ToVector3() => new(x, y, z);
}

[Serializable]
public class FaceDataFrame
{
    public float time;
    public int trackingState;
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public EyeRecord leftEye;
    public EyeRecord rightEye;
    public EyeRecord gazeFixation;
    public BlendShapeRecord[] blendShapes;
    public int vertexCount;
    public string verticesB64;
    public string normalsB64;
    public string indicesB64;
    public string uvsB64;
}

[Serializable]
public class FaceRecordingData
{
    public string recordedAt;
    public int estimatedFrameRate;
    public int frameCount;
    public List<FaceDataFrame> frames = new();
}