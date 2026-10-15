# arkit-demo

A Unity project (AR Foundation + Apple ARKit) for seeing what ARKit face
tracking gives you on an iPhone or iPad. It shows the face mesh, the eye
transforms, the gaze fixation point and every ARKit blend-shape weight. It can
also record a session to JSON and play it back.

Nothing in this README was built or run here (Linux, no Unity or Xcode). The
steps come from the project's settings files and scripts.

## What it does

`Assets/Scenes/SampleScene.unity` is the only scene in the build. It contains:

| GameObject | Components | What it does |
|---|---|---|
| **AR Session** | `ARSession`, `ARFaceManager`, `XROrigin` | Runs ARKit. Tracks at most one face and spawns `Assets/Prefabs/FaceMeshPrefab.prefab` (an `ARFaceMeshVisualizer` with a translucent material) on it. |
| **FaceProber** | `FaceDataProbe.cs` | Every frame, draws a full-screen text report. See the first list below. |
| **FaceDataManager** | `FaceRecorder.cs`, `FacePlayer.cs`, `BlendShapeUI.cs` | Records and replays face data, and drives the debug canvas. See the second list below. |
| **FaceDebugCanvas** | Screen-space UI | A status line, the blend-shape list, Record / Stop / Play / Pause / Load buttons and a seek slider. |

The **FaceProber** report shows:

- the requested and current camera facing direction
- the number of tracked faces
- for each face:
  - tracking state and pose
  - mesh vertex, triangle and UV counts
  - left and right eye pose
  - gaze fixation point
  - every blend-shape weight, by name

The **FaceDataManager** scripts:

- `FaceRecorder` captures up to 60 frames per second while recording. Each frame
  holds the pose, the eyes, the fixation point, the blend shapes and the full
  face mesh (vertices, normals, indices and UVs, base64-encoded). **Stop**
  writes it to `face_recording.json` in `Application.persistentDataPath`.
- `FacePlayer` loads a recording and plays, pauses, seeks and loops it. It
  rebuilds the face mesh and draws eye-direction lines for each frame.
- `BlendShapeUI` fills the canvas: tracking status plus blend shapes with
  weight ≥ 0.1 (up to 30 of them). It also wires up the buttons. **Load** opens
  the newest `.json` in `persistentDataPath`.

Editor-only tools:

- **ARKit Demo → Setup Face Tracking Demo** (`Assets/Editor/FaceDemoSetup.cs`)
  creates the material, the face prefab, FaceDataManager and the debug canvas
  in the open scene.
- `Assets/Editor/iOSBuilder.cs` has `iOSBuilder.Build()` for command-line iOS
  builds.
- `Assets/Scripts/FaceMeshVisualizerExtras.cs` draws eye, gaze, normal and
  bounds gizmos in the Scene view (`OnDrawGizmos`, so Editor only).

`Assets/Plans/ARKitFaceDemo_Plan.md` is the design plan the scripts were written
from.

## Requirements

- **Unity 6000.5.0f1**, the version in `ProjectSettings/ProjectVersion.txt`.
  Install it through Unity Hub with the **iOS Build Support** module.
- Unity downloads these packages from `Packages/manifest.json` on first open:
  - AR Foundation 6.5.0
  - Apple ARKit XR Plugin 6.5.0
  - Universal RP 17.5.0
  - XR Interaction Toolkit 3.5.0
  - Input System 1.19.0
  - Google ARCore XR Plugin 6.5.0 (from the Mobile AR template)
- A **Mac with Xcode** to build and install the iOS app.
- An **iPhone or iPad with a TrueDepth (Face ID) camera**, running iOS 15.0 or
  later (`iOSTargetOSVersionString: 15.0`).
- ARKit face tracking is already enabled under Project Settings → XR Plug-in
  Management → Apple ARKit (`m_FaceTracking: 1`).

## Open the project

1. In Unity Hub, choose **Add → Add project from disk** and select this folder.
   Open it with 6000.5.0f1. The first open rebuilds `Library/` (gitignored) and
   resolves packages, so it takes a while.
2. Open `Assets/Scenes/SampleScene.unity`.

Play mode in the Editor uses the XR Simulation loader, which has no face
tracking, so the face panels stay empty there. Face data only appears on a
device.

## Build to iOS

1. Open **File → Build Profiles**, choose **iOS**, then **Switch Platform**.
2. In **Player Settings**, set your own **Bundle Identifier**. It is still the
   template default, `com.unity.template.armobile`. You can also set a
   **Signing Team ID**, which is currently empty.
3. Click **Build** and choose an output folder such as `build/iOS`
   (`/build/` is gitignored). Unity writes an Xcode project there.

   Or build from the command line. `iOSBuilder.Build` writes to `build/iOS`, or
   to `$IOS_BUILD_PATH` if that is set:
   ```bash
   /Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity \
     -batchmode -quit -projectPath "$(pwd)" \
     -executeMethod iOSBuilder.Build -logFile -
   ```
4. Open `Unity-iPhone.xcodeproj` from the output folder.
5. Select the **Unity-iPhone** target, then **Signing & Capabilities**, and pick
   your team.
6. Connect the device, select it and press **Run**.

### The committed `ios_build/` folder

`ios_build/` is an Xcode project that Unity generated (`Unity-iPhone.xcodeproj`
plus `Il2CppOutputProject/`, `Data/` and so on). It is about 3,200 files and
roughly 750 MB on disk. Its `Data/level0` contains the same scene objects as
`SampleScene`, so it appears to be an export of this project. You could try
opening `ios_build/Unity-iPhone.xcodeproj` directly in Xcode without installing
Unity (untested), but it is build output and may lag behind `Assets/`.
Regenerate it from Unity when in doubt.

## Known issues

These come from reading `SampleScene.unity` and were not tested on a device.

- **The buttons won't respond.** The scene has no `EventSystem`, so the
  Record / Stop / Play / Pause / Load buttons and the seek slider get no touch
  input.
  - Fix: add an Event System (GameObject → UI → Event System) and save the
    scene.
  - Use the Input System UI Input Module, because the project uses the new
    Input System only (`activeInputHandler: 1`).
- **There is no camera.**
  - The `XROrigin`'s Camera field is empty, and the scene has no `Camera`,
    `ARCameraManager` or `ARCameraBackground`.
  - The text overlays still draw: the IMGUI probe and the Screen Space –
    Overlay canvas.
  - The 3D face mesh prefab and `FacePlayer`'s replay mesh and eye lines have
    no camera to render them.
  - `FaceDataProbe`'s "Force Front Camera" option has no `ARCameraManager` to
    switch.
  - Fix: add an AR camera (with `ARCameraManager` and `ARCameraBackground`)
    under the XR Origin and assign it as the origin's camera.
- **No CI.** `.github/workflows/ios-build.yml` is commented out entirely. It was
  written to build with a `UNITY_LICENSE` secret.

## Project structure

```
Assets/
  Scenes/SampleScene.unity   the demo scene
  Scripts/                   FaceDataProbe, FaceRecorder, FacePlayer, BlendShapeUI,
                             FaceDataFrame (JSON data model), FaceMeshVisualizerExtras
  Editor/                    FaceDemoSetup (menu), iOSBuilder (batch build)
  Prefabs/                   FaceMeshPrefab + FaceWireframe material
  XR/                        XR Plug-in Management + ARKit settings
  MobileARTemplateAssets/, Samples/, Settings/, TextMesh Pro/   from the Unity Mobile AR template
Packages/manifest.json       package versions
ProjectSettings/             Unity version, player settings
ios_build/                   generated Xcode project (build output, see above)
```
