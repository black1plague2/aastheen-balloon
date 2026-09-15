# Verse — Multi-Game VR Rehabilitation Platform

**Unity 6 / Meta Quest 3S client for Aastheen**, a clinical tele-rehabilitation system. One
Quest headset runs five distinct rehab mini-games behind a single patient-facing shell; a
companion Flutter app (clinician-side, separate repo) prescribes sessions, streams live
telemetry, and reviews clinical reports over WebSocket.

> Repo name is legacy (`aastheen-balloon`, the first game shipped) — the project has since
> grown into the full multi-game "Verse" platform described below.

## What's inside

| Scene | Game | Clinical purpose |
|---|---|---|
| `ballooon.unity` | **Balloon Pop** | Forearm rotation range-of-motion — pop balloons by rotating to a prescribed angle and holding |
| `Game_Garden.unity` | **Garden** | Bilateral fine-motor tasks — watering rows and sorting seeds by type |
| `paint.unity` | **Paint Wall** | Reach and shoulder mobility — paint a wall surface across a full range of motion |
| `Game_Trophy.unity` | **Trophy Forge** | Hand-pose rehab — assemble a trophy by matching recorded hand poses (open hand, grip, pinch) |
| `Game_Pose.unity` | **Pose** | General pose logging/detection game used for broader mobility tracking |
| `Bootstrap.unity` | — | PIN-code patient login + multi-game session launcher |

A single session can chain any combination of these games in a clinician-defined order
(`PlaylistManager`), so one login covers a full multi-exercise prescription instead of one
game at a time.

## Architecture

```
Flutter clinician app  <──WebSocket (JSON)──>  BootstrapManager / VerseClient
                                                        │
                                                launches one of five
                                                Unity game scenes
                                                        │
                        GameManager / DifficultyManager / USNManager
                        GazeTracker / SequenceManager / ClinicalReportScreen
```

- **`BootstrapManager`** — 5-digit PIN entry, resolves the patient + prescribed game
  sequence from the backend, and fixes a recurring Meta XR SDK gotcha where a bare
  `OVRCameraRig` (no Interaction SDK rig) leaves world-space UI canvases unclickable —
  it detects the active rig and wires `PointableCanvas`/`GraphicRaycaster` or falls back to
  `OVRInputModule`/`OVRRaycaster` automatically at startup.
- **`WSClient` / `VerseClient`** — WebSocket link to the clinician backend: UDP-broadcast
  discovery on the local subnet so the headset finds the server without a hard-coded IP,
  automatic reconnect, and typed JSON payloads (`VerseDataModels`) for prescriptions,
  live metrics, and session completion.
- **`GameManager`** — per-session state machine shared by the exercise games: score, reps,
  session timer, and a full clinical **prescription contract** (target rotation angle, hold
  time, rep count, balloon size range, spawn interval, session duration, distractor count,
  sequence length, reaction-time limit) driven entirely by data pushed from the clinician app
  rather than hard-coded in Unity.
- **`DifficultyManager`** — adaptive difficulty: a sliding 5-trial accuracy window adjusts
  spawn interval, target size, and speed multipliers up or down each wave, so the exercise
  keeps pace with the patient instead of using fixed levels.
- **`USNManager` / `GazeTracker`** — unilateral spatial neglect (USN) mode: tracks the
  fraction of time gaze/attention spends left vs. center vs. right of the play space, feeding
  a clinical neglect-severity signal alongside standard CBS/MPT scores.
- **`SequenceManager`** — order-recall exercises (pop balloons in a clinician-set sequence),
  with a wrong-order flash cue.
- **`ClinicalReportScreen`** — end-of-session summary (accuracy, reaction times, correct vs.
  missed vs. incorrect) handed back to the clinician app for the patient's record.

## Tech stack

Unity 6, C#, Meta XR / Oculus Interaction SDK (Quest 3S hand + controller input),
`NativeWebSocket` for the clinician link, TextMeshPro UI, custom UDP subnet discovery for
zero-configuration pairing with the Flutter app on the same network.

## Related repos

- Flutter clinician app (session prescription, live telemetry, patient records) — companion
  project, not in this repository.
- [`aastheen`](https://github.com/Nainikap/aastheen) — collaborator's Flutter workspace this
  headset app pairs with.

## Building

Open in Unity 6 (Android/Quest build target), install the Meta XR SDK packages listed under
`Packages/`, and build any of the scenes under `Assets/Scenes/` to a Quest 3S. `Bootstrap.unity`
is the entry point for the full multi-game flow; the individual game scenes can also be run
standalone for testing.
