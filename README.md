# Input Repeater

A small Windows app that records mouse activity, saves it locally, and plays it back with user-chosen rules. Keyboard recording is optional.

## Build

```powershell
.\build.ps1
```

The build uses the .NET Framework compiler that ships with Windows:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

## Run

```powershell
.\InputRepeater.exe
```

Controls:

- The app starts in the Windows notification area instead of opening the big window.
- Left-click the Input Repeater icon for a SwiftUI-inspired control popup with rounded cards and capsule buttons.
- Right-click the Input Repeater icon for a reliable Windows menu with Record, Stop, Play, Full Window, and Exit.
- Double-click the Input Repeater icon to show the full window.
- The tray icon changes color for idle, recording, and playing.
- The full window has a theme picker. Current themes: Swift Light, Swift Dark, and Frost Blue.
- The full window is a compact macro remote. Mouse coordinates are not shown in the normal interface.
- The logo is embedded into `InputRepeater.exe`; no separate image file is needed to run the app.
- `F8`: start or stop mouse recording.
- `F9`: play the loaded or recorded actions.
- `F12`: stop while playing.
- `Also record keyboard`: include keyboard input in the recording when you need it.

Mouse start point:

- When recording starts, the cursor moves to the center of the current screen.
- New recordings save mouse movement from that center point.
- When playing, the cursor moves to the center again before the saved movement starts.
- Mouse positions are stored at full pixel detail and played with DPI-aware absolute screen coordinates.
- Mouse movement over the Input Repeater window is recorded too.
- Use `F8` to stop recording if you do not want a click on the app window saved into the macro.
- Older saved files still play with their original screen positions.

Play rules:

- Times to play.
- Stop after seconds.
- Wait before start.
- Speed percent.
- Only play in a chosen window.
- Never play in blocked windows.

Window:

- Drag the window edges to choose a comfortable size.
- The app remembers the last window size and position.
- Use `Reset Size` to return to the default size.

Browser focus:

- `F9` is handled by both a normal Windows hotkey and a backup keyboard watcher, so play should still start when Chrome, Edge, Firefox, or another browser is active.

Performance:

- While the full window is hidden, recorded events are stored without refreshing the large event list.
- Mouse movement is sampled to reduce duplicate high-frequency move events. Clicks, wheel movement, and button presses are still recorded directly.
- When keyboard recording is off, the recorder does not install the extra recording keyboard hook.

## Disclaimer

This tool is intended for local, visible, user-controlled automation. It does not hide itself, persist in the background, send data anywhere, or record unless recording is started by the user. Avoid recording passwords, private messages, payment flows, or anything you would not want stored in a local macro file.
