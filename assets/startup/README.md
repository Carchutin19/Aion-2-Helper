# Own startup placeholder

`blank-intro.bk2` was generated independently from a solid black image created
by `CreateIntroVideo.ps1`. No footage, scripts or binary data from another mod
were used. It contains one 64×64 black frame at 60 FPS, without audio.

Developer tool: the existing Unreal Engine 5.2
`Engine/Binaries/ThirdParty/Bink/Bink2ForUnreal.exe`, `Bink2c` command.
The encoder itself is not distributed. See [Epic's Bink documentation](https://dev.epicgames.com/documentation/en-us/unreal-engine/bink-video-for-unreal-engine).

Generated asset: 76 bytes, SHA-256
`afbf665659bda86a01e139cf872f8e007591935ae8805eeac0ff9b0d9908b307`.
The asset is embedded in the helper executable. A valid short video preserves
the file expected by startup playback. A click-coordinate issue reported during
earlier testing was resolved by matching the game resolution to the active
display; it was reproduced with the original intro and the helper closed.
Startup, intro skipping and correct mouse input with this replacement were
verified locally on 12 October 2026.
