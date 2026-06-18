# Aastheen — VR Rehabilitation Platform (Meta Quest)

Unity project for the Aastheen rehabilitation system. Contains all VR games for Meta Quest 3S, designed to work alongside the Aastheen Flutter companion app and Verse cloud backend.

---

## Scenes

| Scene | Purpose |
|---|---|
| `Assets/Scenes/Bootstrap.unity` | Verse entry point — 5-digit PIN keypad, server URL settings, OVR controller input |
| `Assets/Scenes/ballooon.unity` | Balloon Pop game — clinical cognitive + motor rehabilitation |
| `Assets/Scenes/Game_Garden.unity` | Garden Rehab game — upper-limb motor rehabilitation with gardening tools |

All three scenes are registered in EditorBuildSettings and can be loaded in sequence by the playlist system.

---

## Architecture

```
Flutter app (doctor/nurse)
    │
    ├─ REST  ──► Verse backend (cloud FastAPI + Postgres)
    │               │
    │               └─ prescription codes, session results, EMG data
    │
Quest headset
    ├─ Bootstrap scene: enters 5-digit code → backend returns prescription
    ├─ GameManager: reads PlaylistManager → loads correct game scene
    ├─ Balloon / Garden game: plays session, collects metrics
    └─ FinalSummary: POSTs results to backend via VerseClient
```

**V1 (legacy):** Quest connects directly to Flutter as WS client via UDP auto-discovery.  
**V2 (Verse):** Quest authenticates with a 5-digit prescription code against the cloud backend; home mode (direct HTTPS) and clinic mode (Nurse WS relay) supported.

---

## Scripts

### Verse Integration (`Assets/Scripts/Verse/`)

| Script | Role |
|---|---|
| `VerseClient.cs` | HTTP client for all backend REST calls; `BaseUrl` persisted to PlayerPrefs |
| `VerseDataModels.cs` | All API data models (BalloonSettings, GardenSettings, GameMetrics, payloads) |
| `PlaylistManager.cs` | DontDestroyOnLoad; holds active prescription + current game index |
| `BootstrapManager.cs` | PIN keypad UI logic, server URL settings panel, OVR input wiring |

### Balloon Pop (`Assets/Scripts/`)

| Script | Role |
|---|---|
| `GameManager.cs` | Session lifecycle, V1 WS + V2 Verse dual-mode |
| `BalloonSpawner.cs` | Spawns balloons by mode (standard / sequence / reaction / USN) |
| `Balloon.cs` | Per-balloon state, trigger detection, pop logic |
| `WSClient.cs` | V1: connects to Flutter WS server via UDP discovery |
| `WSDataModels.cs` | V1 message models |

### Garden Rehab (`Assets/Scripts/Garden/`)

| Script | Role |
|---|---|
| `GardenGameManager.cs` | Verse wrapper — applies prescription settings, drives PlantTaskManager, POSTs results |
| `GameIntroUI.cs` | 6-slide world-space tutorial shown before each session |
| `Rehab/PlantTaskManager.cs` | Core task sequencer — picks plants, assigns tools, tracks completion |
| `Rehab/TaskInstructionUI.cs` | Rehab HUD: task badge, tool colour, hold progress bar, complete panel |
| `Rehab/PlantInteractable.cs` | XR grab detection + proximity hold logic |
| `Rehab/RehabProgressManager.cs` | Session scoring and progress tracking |
| `Rehab/RehabAutoWire.cs` | Editor helper — auto-links scene references at runtime |
| `Rehab/ToolSelector.cs` | Manages which tool is currently active |

### Editor Utilities (`Assets/Editor/`)

| Script | Role |
|---|---|
| `BootstrapSceneSetup.cs` | Procedurally creates the Bootstrap scene (OVRCameraRig, keypad UI, settings panel) |
| `GardenUIWire.cs` | Garden UI wiring menu — patches TaskPanel, CompletePanel, creates IntroCanvas |
| `AARNamespacePatcher.cs` | Fixes Meta XR SDK duplicate namespace before every Android build |

---

## Verse Data Flow

```
Bootstrap PIN entry
    │ POST /prescriptions/verify {"code": "12345"}
    ▼
Backend returns: session_token + prescription (game list + settings)
    │
    ▼ PlaylistManager stores prescription
GameManager reads game[0] → loads scene
    │
Game runs → collects metrics
    │ POST /sessions/{session_id}/results
    ▼
Backend stores results; Flutter app shows session report
```

---

## Balloon Clinical Modes

| Mode | Description |
|---|---|
| `standard` | Balloons spawn randomly; pop with PinTip |
| `sequence` | Numbered balloons must be popped in order (wrong = red flash) |
| `reaction` | Balloons expire after `reactionTimeLimit` seconds |
| `usn` | USN rehabilitation — weighted spawning to left/centre/right zones |

**Adaptive difficulty:** enabled via prescription; game auto-adjusts balloon size and spawn interval based on performance.

---

## Building

1. Open project in Unity 2022.3 LTS
2. File → Build Settings → Android → switch platform
3. Add scenes in order: Bootstrap → ballooon → Game_Garden
4. Player Settings: set package name, min API 29, target API 32
5. Build → install via `adb install <file>.apk` or SideQuest

> The `AARNamespacePatcher` runs automatically before every Android build to fix the Meta XR SDK namespace conflict.

---

## Flutter Companion App

Repo: `Nainikap/aastheen` · Branch: `flutter-ws-server`  
Entry points: `lib/main.dart` (V1) · `lib/main_v2.dart` (Verse V2)

## Backend

Repo: `aashteen_backend` · Deployed: `https://aastheen.onrender.com`  
Docs: `GET /docs` on a local instance
