# City Walk

A Unity prototype of a commuter's walk round a city park: walk laps, grab a coffee at the kiosk each lap, then head to the office. Built from `CityWalk_Starter.zip` to match the web preview video.

## Requirements

- Unity **6000.3.2f1**
- Universal Render Pipeline (URP 17.3)
- Android Build Support (SDK, NDK, OpenJDK) to build the APK

## Open and play

1. Open the project folder in Unity Hub with 6000.3.2f1.
2. Open `Assets/CityWalk/Scenes/Home.unity` and press Play, then press **Play** on the home screen.
3. To go straight into the game, open `Assets/CityWalk/Scenes/CityWalk.unity`. If it is missing, run **City Walk > Create Starter Scene**.

## Controls

| Action | Phone | Keyboard |
|---|---|---|
| Walk | On-screen joystick | WASD or arrow keys |
| Brisk walk | Push the joystick all the way | Hold Left Shift |

## Features

- 3D home screen with character select and settings (music and sound effects)
- Lap loop round the park with a coffee kiosk stop each lap
- Lap time limit with a Mission failed screen
- Pedestrians walking the city
- Morning lighting, generated textures and colour grade
- Mobile tuned: URP mobile settings, static batching, frame pacing

## Project layout

| Path | Contents |
|---|---|
| `Assets/CityWalk/Scripts` | Game code |
| `Assets/CityWalk/Editor` | Editor tools |
| `Assets/CityWalk/Scenes` | Home and game scenes |
| `Assets/CityWalk/Art`, `Audio`, `UI` | Textures, sounds and HUD sprites |
| `Assets/Character_Models` | Player and pedestrian characters |
| `Assets/Settings` | URP pipeline and renderer assets |

## Assets and licences

No paid assets are used.

| Asset | Source |
|---|---|
| Starter scripts and scene setup | `CityWalk_Starter.zip`, supplied with the task |
| Sound effects, textures and HUD sprites | Made for this project |
| Ground, facade and sky textures | Generated in code at runtime (`CityTextures.cs`) |
| Character models | Quaternius (CC0) |

## Build for Android

**File > Build Profiles > Android > Switch Platform**, then **Build**. Builds go in `_Build/`, which is not committed.
