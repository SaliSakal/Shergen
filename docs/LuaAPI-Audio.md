# Audio

*Core — compiled into the main `shergen` project (`Shergen.Audio.cs`), not a separate module.*

Built on OpenAL (`Silk.NET.OpenAL`). Supports `.wav` and `.ogg`. "Channels" are volume/3D groups; "sounds" are individual playable sources.

Two layers, same pattern as `GUI`: the raw `AUDIO.*` bridge below, and the `Sound` Lua wrapper on top of it (`Shergen/core/sound.lua`) for the common case of "play this file and clean up when it's done."

---

## `Sound` wrapper (recommended for simple playback)

```lua
Sound = ListClass.Make();          -- registry of currently-playing sounds
VOLUME_DEFAULT = AUDIO.AddChannel(); -- default channel, created automatically on load
Sound.opVID = VOLUME_DEFAULT;
```

| Function | Description |
|---|---|
| `Sound.Play(filename [, volumeID, callback, loop, pitch])` → id | Loads (streamed) and immediately plays `filename` on `volumeID` (or `VOLUME_DEFAULT` if omitted), registers it in the `Sound` list, and returns its registry ID (**not** the same as the raw `AUDIO` source ID). |
| `Sound.Finish(id)` | Called automatically when a sound finishes playing (wired up as the `AUDIO.LoadSound` finish-callback) — frees the source and removes it from the registry. Not normally called manually. |

`callback` (passed to `Sound.Play`) fires from `Sound.Finish` once playback ends, and can be either a plain function (called with no arguments) or `{func, ...args}` (called as `func(...args)`).

```lua
Sound.Play("sfx/click.ogg");
Sound.Play("music/theme.ogg", musicChannel, function() print("theme finished") end, true); -- looped
```

For anything beyond "play and forget" (pausing, seeking, 3D positioning, per-sound pitch changes while playing), use the raw `AUDIO.*` functions below directly with the source ID returned by `AUDIO.LoadSound`.

---

## AUDIO (raw bridge)

| Function | Description |
|---|---|
| `AUDIO.AddChannel()` → id | Creates a new channel. |
| `AUDIO.SetChannelVolume(channelID, volume)` / `GetChannelVolume(channelID)` | Volume is `0-1000` (mapped internally to `0.0-1.0` gain, clamped to `1500`). |
| `AUDIO.SetChannel3D(channelID, bool)` / `GetChannel3D(channelID)` | Toggles positional (3D) audio for the channel. |
| `AUDIO.SetChannelDistance(channelID, min, max)` / `GetChannelDistance(channelID)` → min, max | Attenuation distance range for 3D channels. |
| `AUDIO.LoadSound(path [, channelID, stream, loop, pitch, callback])` → sourceID | Loads and creates a playable source on a channel. `pitch` is `100` = normal speed. `callback` fires when playback finishes. |
| `AUDIO.PlaySound(sourceID [, fromStart])` | Plays (or resumes) a source; `fromStart` (default `true`) rewinds first. |
| `AUDIO.PauseSound(sourceID)` / `AUDIO.StopSound(sourceID)` | Pauses or stops playback. |
| `AUDIO.FreeSound(sourceID)` | Stops and releases the source and its buffer. |
| `AUDIO.IsPlaying(sourceID)` | Returns `true`/`false`. |
| `AUDIO.GetSoundTime(sourceID)` / `AUDIO.SetSoundTime(sourceID, seconds)` | Playback position. |
| `AUDIO.SetSoundPosition(sourceID, x, y, z)` / `AUDIO.SetSoundVelocity(sourceID, x, y, z)` | 3D position/velocity (for channels with `Is3D` set). |
| `AUDIO.SetSoundPitch(sourceID, pitch)` | `100` = normal speed. |
| `AUDIO.SetSoundLoop(sourceID, bool)` | Toggles looping. |

`AUDIO.VERSION` is a constant string (currently `"1.0"`).
