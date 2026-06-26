# ARKit Face Tracking Demo — Implementation Plan

## Overview
Demo to visualize, record, and playback ARKit face tracking data (face mesh + 52 blendshapes) on iOS via AR Foundation 6.5.0.

## Scene Setup

### GameObjects to create in SampleScene.unity

1. **FaceDataManager** (root GameObject)
   - Attach: `FaceRecorder.cs`, `FacePlayer.cs`
   - Purpose: Orchestrates record/playback; survives across the demo

2. **FaceDebugCanvas** (child of XR Origin or screen-space)
   - Canvas (Screen Space - Overlay), Canvas Scaler
   - Panel with ScrollRect → Content with TMP Text for blendshape display
   - Buttons: Record, Stop, Play, Pause, Load File
   - Attach: `BlendShapeUI.cs`
   - Purpose: Live blendshape weight display, record/playback controls

3. **FaceMeshPrefab** (prefab asset)
   - GameObject with: `MeshFilter`, `MeshRenderer`, `ARFaceMeshVisualizer`
   - Material: translucent wireframe (URP Lit with custom shader or just transparent)
   - Attach: `FaceMeshVisualizerExtras.cs`
   - Purpose: Per-face mesh rendering; assigned to ARFaceManager.facePrefab

## Scripts

### FaceDataFrame.cs
Serializable data structures:
- `BlendShapeRecord`: id, name, weight
- `EyeRecord`: position, rotation
- `FaceDataFrame`: timestamp, trackingState, pose, eyes, blendShapes[], vertexCount, vertices/base64, normals/base64, indices/base64, uvs/base64
- `FaceRecordingData`: top-level wrapper with metadata (recordedAt, frameRate, frameCount) + frames[]

Uses base64 for mesh data (float[] → byte[] → base64) to keep JSON reasonable.

### FaceRecorder.cs
- Attached to FaceDataManager
- References ARFaceManager
- Record(): clears buffer, sets isRecording = true
- Update(): if recording, captures from ARFaceManager.trackables each frame
- StopRecording(): serializes List<FaceDataFrame> to JSON, writes to Application.persistentDataPath
- Saves both an uncompressed JSON and optionally a binary .facebin file
- Exposes: IsRecording, FrameCount, RecordingTime, OnRecordingFinished event

### FacePlayer.cs
- Attached to FaceDataManager
- Loads FaceRecordingData from file
- Play/Pause/Stop/Seek controls
- Creates a playback GameObject with SkinnedMeshRenderer for blendshape replay
- Creates a playback mesh GameObject for vertex replay
- Interpolates between recorded frames for smooth playback
- Exposes: IsPlaying, CurrentTime, TotalTime, Progress (0-1)

### BlendShapeUI.cs
- Attached to FaceDebugCanvas
- References: ARFaceManager, TMP_Text for blendshapes, TMP_Text for status
- References: FaceRecorder, FacePlayer for button callbacks
- Updates blendshape list every frame from currently tracked faces
- Shows tracking state, face count, active blendshapes with weights
- Buttons wired to: Record/Stop, Play/Pause, Load Recording

### FaceMeshVisualizerExtras.cs
- Attached to face prefab
- Optional: colors mesh vertices, adds debug raycasts for eyes, draws gaze point
- Self-contained; works alongside ARFaceMeshVisualizer

## Data Flow

```text
Recording:
  ARFaceManager.trackables → FaceRecorder.Update()
    → extract per-face: blendshapes, mesh, eyes, pose
    → append FaceDataFrame to buffer
  Stop → serialize List to JSON/binary → write to disk

Playback:
  FacePlayer.Load() → deserialize FaceRecordingData
  FacePlayer.Update() → advance time, binary-search frames
    → apply blendshapes to SkinnedMeshRenderer head model
    → apply mesh vertices to playback Mesh
```

## Dependencies
- AR Foundation 6.5.0 (installed)
- ARKit 6.5.0 (installed)
- TextMeshPro (installed)
- XR Interaction Toolkit 3.5.0 (installed)
- iOS TrueDepth device (iPhone X or newer, or M-series iPad Pro)
- Project Settings → XR Plug-in Management → Apple ARKit → Face Tracking: enabled (already done)