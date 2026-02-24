# Piximate

A lightweight, code-driven sprite animation system for Unity 2D projects. Piximate gives you a clean runtime animator, a reusable ScriptableObject clip format, and a polished Editor window so you can build and preview animation clips without leaving Unity.

---

## Features

- **`AnimClip`** — a ScriptableObject that stores an ordered array of sprites, a frame rate, and a loop flag. Clips are first-class project assets you can share across multiple objects.
- **`Piximator`** — a `MonoBehaviour` that drives a `SpriteRenderer` at runtime. Supports multiple named clips, adjustable playback speed, and C# events for reacting to animation milestones.
- **Pixiditor** — a custom Editor window (`Window → PixelCut → Anim Clip Editor`) for assembling clips visually. Drag sprites in, reorder them, preview playback in real time, then save the clip as an asset in one click.

---

## Installation

### Via Unity Package Manager (git URL)

1. In Unity open **Window → Package Manager**.
2. Click the **＋** button and choose **Add package from git URL**
3. Paste the git URL "https://github.com/AhmedGD1/Piximate.git"

### Manual

Download it then paste it in the asset folder

---

## Quick Start

### 1 — Create an AnimClip asset

**With the Pixiditor editor window:**

1. Open **Window → Piximate → Anim Clip Editor**.

<img width="519" height="577" alt="image" src="https://github.com/user-attachments/assets/32dd118f-0fa0-4739-a983-a4cf941f8a95" />

2. Drag your sprites into the **Frames** drop zone (or use the object-picker cells).

<img width="382" height="626" alt="image" src="https://github.com/user-attachments/assets/f0474e68-49d4-4cbd-bee7-d16e455deaba" />

3. Set a **Clip Name**, **Frame Rate**, and whether it should **Loop**.

4. Choose a save folder and press **Save AnimClip Asset**.

<img width="362" height="156" alt="image" src="https://github.com/user-attachments/assets/dc2c2ef6-4080-4dbf-a866-87f1302aeaec" />


**Manually:**

Right-click in the Project window and choose **Create → Piximate → Animation Clip**.

<img width="752" height="220" alt="image" src="https://github.com/user-attachments/assets/67c1d15f-844b-4599-9e33-bbd7ef763e4d" />

### 2 — Add a Piximator to your GameObject

1. Attach the `Piximator` component to any GameObject that has a `SpriteRenderer`.

<img width="229" height="196" alt="image" src="https://github.com/user-attachments/assets/f56d2566-4862-4953-af2c-c5f86635bd69" />


2. Assign the `SpriteRenderer` reference in the Inspector.
3. Add one or more `AnimClip` assets to the **Anim Clips** list.

<img width="438" height="187" alt="image" src="https://github.com/user-attachments/assets/1c30ca70-1d63-484e-9392-f29271b8c585" />

### 3 — Play clips from code

```csharp
// Play by clip asset name
piximator.Play("Run");

// Force-restart the same clip
piximator.Play("Run", forceReset: true);

// Stop playback
piximator.Stop();

// Change playback speed (1 = normal, 2 = double speed, etc.)
piximator.SetPlaybackSpeed(1.5f);
```

### 4 — React to animation events

```csharp
piximator.AnimationFinished += OnAnimFinished;
piximator.AnimationLooped   += OnAnimLooped;
piximator.FrameChanged      += OnFrameChanged;

void OnAnimFinished(string clipName) { /* non-looping clip ended */ }
void OnAnimLooped(string clipName)   { /* looping clip wrapped */ }
void OnFrameChanged(int frameIndex)  { /* a new frame was displayed */ }
```

### 5 — Register clips at runtime

Clips listed in the Inspector are registered automatically on `Awake`. You can also register and unregister clips dynamically:

```csharp
piximator.RegisterClip(myRuntimeClip);
piximator.UnRegisterClip(myRuntimeClip);
```

---

## API Reference

### `AnimClip` (ScriptableObject)

| Member | Description |
|---|---|
| `Sprite[] Frames` | The ordered frame sprites. |
| `float FrameRate` | Frames per second. Default: `10`. |
| `bool Loop` | Whether the clip loops. Default: `true`. |
| `SetFrames(Sprite[])` | Set frames at runtime or from the Editor. |
| `SetFrameRate(float)` | Set the frame rate. |
| `SetLoop(bool)` | Set the loop flag. |

### `Piximator` (MonoBehaviour)

| Member | Description |
|---|---|
| `string CurrentAnimation` | Name of the currently active clip, or empty. |
| `int CurrentFrames` | Total frames in the active clip. |
| `bool IsPlaying` | `true` while a clip is active. |
| `Play(string)` | Play a clip by name. No-ops if already playing. |
| `Play(string, bool)` | Play a clip; pass `true` to force restart. |
| `Stop()` | Stop playback and reset state. |
| `SetPlaybackSpeed(float)` | Multiplier applied to delta time. Minimum: `0.01`. |
| `RegisterClip(AnimClip)` | Add a clip to the runtime dictionary. |
| `UnRegisterClip(AnimClip)` | Remove a clip from the runtime dictionary. |
| `event AnimationFinished` | Fired when a non-looping clip completes. |
| `event AnimationLooped` | Fired each time a looping clip wraps. |
| `event FrameChanged` | Fired every time the displayed frame changes. |

---

## Pixiditor — Editor Window

Open via **Window → PixelCut → Anim Clip Editor**.

| Section | Description |
|---|---|
| **Preview** | Live preview of the clip with play/pause/step controls and a checkerboard background. |
| **Settings** | Clip name, frame rate slider, and loop toggle. |
| **Frames** | Drag-and-drop sprite list. Supports reorder by drag, duplicate, and remove per frame. Also accepts multi-sprite drag from the Project window. |
| **Save** | Folder picker and one-click asset creation. Folders that don't exist are created automatically. |

---

## Requirements

- Unity **2021.3** or newer (uses `Dictionary.TryAdd` and tuple deconstruction).
- The `Piximator` component requires a `SpriteRenderer` on the same GameObject or assigned manually in the Inspector.

---

