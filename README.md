# Kooby
**A 3×3×3 matrix board game — bring your koobs together to win**

[![Unity](https://img.shields.io/badge/Unity-2022.3+-blue)](https://unity.com)

A Unity project for **Kooby**, a turn-based 3D board game played on a 3×3×3 matrix. Each of four players controls two **koobs** (pieces) starting at opposite corners. The goal is to maneuver both koobs into adjacent cells. Optional **bump** mechanics — pushing an opponent's piece when a landing space exists beyond them — are being added next.

The repo is intentionally focused: one main scene, a logical board state, animated piece movement, AI opponents, UI Toolkit HUD controls, and shader-based outline highlights.

---

## Goal

Build a playable **Kooby** experience on a 3×3×3 matrix:

- Four players, two pieces each, starting at opposite corners of the cube.
- Legal moves depend on a piece's position type (corner, edge, outer center, or middle center).
- **Win condition:** both of a player's pieces occupy adjacent cells (Manhattan distance = 1).
- Human players cycle through legal destinations and confirm moves via the HUD.
- AI opponents use a lightweight **GOAP** planner to bring their pieces together.
- **Bump** support: face-based squash animation is implemented; bump-move detection is tracked per turn; full bump-as-action gameplay is next.

---

## Current Status

- **Scene:** `Assets/Scenes/Main.unity` — matrix, four players, HUD, camera orbit, highlights, and outline post-effect.
- **Board logic:** `KoobState` tracks occupancy and computes legal moves per piece geometry.
- **Human input:** UI Toolkit HUD with left / move / right buttons; virtual thumbstick drives camera orbit.
- **AI:** `NPCPlayer` + `GOAPPlanner` with `BringPiecesTogetherGoal`.
- **Bump animation:** `BumpAnimation` — temporary pivot reparent, `ScriptableCurve`-driven axis scale squash per face.
- **Bump move tracking:** `currentPossibleBumpMoves` populated each human turn from `KoobState.GetPossibleBumpMovesForPlayer`.
- **Next up:** bump action optionality — choosing to bump an opponent instead of moving your own piece.

---

## Milestones

| Version | Scope | Status |
| --- | --- | --- |
| **Milestone 1** | 3×3×3 board state, piece movement, win detection | ✅ Implemented |
| **Milestone 2** | UI Toolkit HUD, move cycling, highlights | ✅ Implemented |
| **Milestone 3** | GOAP AI opponents | ✅ Implemented |
| **Milestone 4** | Outline SDF post-effect, camera orbit, virtual thumbstick | ✅ Implemented |
| **Milestone 5** | Face-based bump squash animation (`BumpAnimation`) | ✅ Implemented |
| **Milestone 6** | Bump-move detection (`currentPossibleBumpMoves`) | ✅ Implemented |
| **v1.0.0** | Bump action gameplay — bump opponent instead of moving | 🚧 In progress |

---

## Milestone 1 — Core Board & Movement

### Assets

| Asset | Path | Role |
| --- | --- | --- |
| Board state | `Assets/Scripts/KoobState.cs` | 3×3×3 occupancy matrix, move generation, win-position hints |
| Node positions | `Assets/Scripts/KoobNodeSet.cs` | Maps logical `(x,y,z)` indices to world positions |
| Piece | `Assets/Scripts/PlayerPiece.cs` | Curve-driven movement, position tracking, move events |
| Game orchestration | `Assets/Scripts/GameManager.cs` | Spawns matrix/players, turn flow, move execution, win checks |
| Turn state | `Assets/Scripts/GameStateMachine.cs` | `PlayState` / `TurnState`, `NewTurnBegan` event |
| Curve asset | `Assets/Scripts/ScriptableCurve.cs` | Reusable `AnimationCurve` ScriptableObject |
| Matrix prefab | `Assets/Prefabs/Matrix.prefab` | Visual 3×3×3 grid |

### Starting Positions

Each player starts with two pieces at opposite corners:

| Player | Piece A | Piece B |
| --- | --- | --- |
| 1 | `(0,0,0)` | `(2,2,2)` |
| 2 | `(2,0,0)` | `(0,2,2)` |
| 3 | `(0,2,0)` | `(2,0,2)` |
| 4 | `(0,0,2)` | `(2,2,0)` |

### Move Count by Position Type

| Position type | Axes at center (`1`) | Legal destinations |
| --- | --- | --- |
| Corner | 0 | 3 |
| Edge | 1 | 4 |
| Outer center | 2 | 5 |
| Middle center | 3 | 6 |

Only **unoccupied** destinations are returned by `GetPossibleMoves`. Occupied cells are excluded from `currentPossibleMoves`.

### Win Detection

- **Actual win:** both pieces of the current player are adjacent (`|dx| + |dy| + |dz| == 1`) after a move completes.
- **Win soon hint:** if both pieces can legally move to the same destination on the current turn, `CurrentPlayerWillWinSoon` fires with that position.

---

## Milestone 2 — HUD & Human Input

### Assets

| Asset | Path | Role |
| --- | --- | --- |
| HUD controller | `Assets/Scripts/HUDManager.cs` | Turn label, button visibility, move confirmation |
| HUD layout | `Assets/UI/HUD.uxml` | UI Toolkit structure |
| HUD styles | `Assets/UI/HUD.uss` | Button styling, turn label color |
| Highlights | `Assets/Scripts/HighlightsManager.cs` | Pooled highlight meshes at legal destinations |
| Virtual joystick | `Assets/Scripts/Input/VirtualJoystick.cs` | Touch / mouse thumbstick via UI Toolkit |
| Input coordinator | `Assets/Scripts/Input/InputManagerUIToolkit.cs` | Input System action maps |
| Camera orbit | `Assets/Scripts/CameraOrbit.cs` | Orbits around matrix from thumbstick input |

### Scene Setup

1. Open **Kooby** in Unity Hub.
2. Open `Assets/Scenes/Main.unity`.
3. Enter Play mode.
4. For human-controlled players, use the HUD and thumbstick as described below.

### Runtime Controls

| Input | Action |
| --- | --- |
| **Left arrow button** | Cycle `currentMoveChoice` backward through legal moves |
| **Right arrow button** | Cycle `currentMoveChoice` forward through legal moves |
| **Move button** | Execute the currently highlighted move (`ExecuteCurrentMoveChoice`) |
| **Virtual thumbstick** | Orbit the camera around the matrix |

HUD buttons are hidden during AI-controlled turns. Per-player AI control is configured on `GameManager` via `aiControlledPlayers`.

### Human Turn Flow

1. `GameStateMachine.NewTurnBegan` fires for the active player.
2. `GameManager` builds `currentPossibleMoves` (unique legal destinations across both pieces).
3. `currentMoveChoice` resets to `0`; a single highlight shows the selected destination.
4. Left / right buttons cycle the highlight; Move confirms the choice.
5. The piece that can reach the selected cell moves along `playerMoveCurve`.

---

## Milestone 3 — AI Opponents

### Assets

| Asset | Path | Role |
| --- | --- | --- |
| NPC controller | `Assets/Scripts/NPCPlayer.cs` | Chooses piece and destination each AI turn |
| GOAP planner | `Assets/Scripts/GOAP/GOAPPlanner.cs` | Greedy single-step plan search |
| Move action | `Assets/Scripts/GOAP/MovePieceAction.cs` | GOAP action for moving one piece |
| Goal | `Assets/Scripts/GOAP/BringPiecesTogetherGoal.cs` | Minimize Manhattan distance between pieces |
| World state | `Assets/Scripts/GOAP/WorldState.cs` | Key/value board and piece state for planning |

### Runtime Behavior

- Enable AI on `GameManager` via `_enableAI`.
- On an AI turn, `GameManager` waits `aiMoveDelaySeconds`, then `NPCPlayer` picks the best `MovePieceAction` toward adjacency.
- All legal moves are highlighted (not cycled) for AI players.

---

## Milestone 4 — Visual Polish

### Assets

| Asset | Path | Role |
| --- | --- | --- |
| Outline effect | `Assets/Scripts/OutlineEffect.cs` | Stencil mask + SDF compute outline on target materials |
| SDF compute | `Assets/Shaders/Resources/ScreenSpaceSDF.compute` | Screen-space distance field for edge rendering |
| Outline shaders | `Assets/Shaders/OutlineMask.shader`, `OutlineMaskSelective.shader`, `MaskComposite.shader` | Mask write and composite passes |
| Player materials | `Assets/Materials/Player1Piece.mat` … `Player4Piece.mat` | Per-player piece colors |

`OutlineEffect` runs as a camera post-process on assigned target materials. `HighlightsManager` places translucent markers at legal move positions.

---

## Milestone 5 — Bump Animation

### Assets

| Asset | Path | Role |
| --- | --- | --- |
| Bump animation | `Assets/Scripts/BumpAnimation.cs` | Face-pivot squash along one local axis |
| Bump test | `Assets/Scripts/BumpTest.cs` | Cycles all six faces on enable (dev / look-dev) |
| Bump curve | `Assets/Prefabs/BumpCurve.asset` | `ScriptableCurve` assigned to pieces at spawn |

### Runtime Behavior

`BumpAnimation` performs a face-directed squash without destroying the piece:

1. Save the piece's parent and local transform.
2. Create a temporary `BumpPivot` at the selected face center (offset by half local extents).
3. Reparent the piece under the pivot (`worldPositionStays: true`).
4. Over `bumpDuration`, evaluate `bumpCurve` from `t = 0` → `1` and set `pivot.localScale` on the face axis — including when start and end curve values are both `1` (the curve shape still drives intermediate scale).
5. Restore the piece to its saved parent and local transform, then destroy only the pivot.

**API:** `Bump(face)`, `BumpEnumerator(face)`, `BumpAsync(face)`, `Stop()`.

**Faces:** `Top`, `Bottom`, `North`, `South`, `East`, `West`.

### Inspector Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| **Bump Curve** | `ScriptableCurve` | — | Axis scale sampled over normalized time `0`–`1` |
| **Bump Duration** | `float` | `1` | Seconds for one bump cycle |

`GameManager` assigns `bumpCurve` to each `BumpAnimation` component when pieces are created.

---

## Milestone 6 — Bump Move Tracking

Bump moves are detected in logic but not yet exposed as a player action. Each human turn, `GameManager` maintains `currentPossibleBumpMoves` alongside `currentPossibleMoves`.

### Algorithm (`KoobState.GetPossibleBumpMoves`)

For each of the six axis directions from a piece's current cell:

1. Check the **adjacent** cell in that direction.
2. If it is **in bounds**, **occupied**, and owned by an **opponent**:
3. Check the cell **immediately beyond** (same direction).
4. If that cell is **in bounds** and **unoccupied**, the adjacent cell is **bumpable** — add it to `currentPossibleBumpMoves`.

`GetPossibleBumpMovesForPlayer` unions bumpable cells across both of a player's pieces (deduplicated).

### GameManager Integration

| Property | Set when | Cleared when |
| --- | --- | --- |
| `currentPossibleMoves` | Human turn begins | AI turn begins |
| `currentPossibleBumpMoves` | Human turn begins (`GetPossibleBumpMovesForPlayer`) | AI turn begins |

Logged at turn start: count of legal moves and bumpable opponent positions.

### Upcoming — Bump Action

Planned next step: let the human player choose between a normal move and a bump when `currentPossibleBumpMoves` is non-empty — cycling bump targets, executing bump logic on the board, and driving `BumpAnimation` on the struck face. Tracking is in place; gameplay wiring is not.

---

## Logging

`KoobyLogManager` provides categorized, toggleable console output:

| Category | Typical output |
| --- | --- |
| `Manager` | Turn flow, highlights, move choice, AI scheduling |
| `Player` | Piece assignment, movement, win events |
| `Matrix` | Board state debug prints |
| `UI_Input` | Thumbstick and input events |
| `UI_Output` | HUD button clicks, turn label updates |

Per-category toggles are exposed on the `KoobyLogManager` component in the scene.

---

## Requirements

- Unity **2022.3** or later
- **Input System** package (`com.unity.inputsystem` — see `Packages/manifest.json`)
- **UI Toolkit** (included with Unity)
- GPU with compute shader support (for `OutlineEffect` SDF pass)

See `Assets/Scripts/Input/README_InputSystemSetup.md` for virtual joystick and Input Actions setup details.

---

## Repository

- **GitHub:** https://github.com/ajcampbell1333/Kooby

---

## Credits

Created by **AJ Campbell**.

_Kooby: bring your koobs together._
