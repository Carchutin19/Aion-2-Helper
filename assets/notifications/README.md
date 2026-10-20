# Notification artwork

Transparent raster reconstructions of the user-supplied Aion 2 UI symbols, created with the built-in ImageGen tool and approved notification designs as references.

- `shugo.png`: round Shugo emblem, shared by both festival reminders.
- `party.png`: two overlapping people; the rear head is hollow.
- `dungeon.png`: person in front of the carved stone dungeon emblem.
- `rift.png`: hollow purple/lilac Spacetime Rift spiral reconstructed from the user-supplied reference; shared by opening, advance and closing notifications and the independent entry countdown.

The dungeon icon was subsequently edited with built-in ImageGen to remove the central crescent moon, replacing it with the matching dark teal stone texture while retaining the person, ring and outer points. Party and dungeon settings previews use the example inviter `Carchutin`; real invitations continue to display their actual sender.

Generation brief: recreate each approved symbol as a standalone high-resolution cutout, preserving its silhouette, with no text, backdrop, notification gradient or glow. The renderer normalizes visible bounds and caches a reduced image. The soft emissive glow, black alpha gradient and localized text are composed separately at runtime. Assets are embedded by `Build.ps1`, so updates cannot leave the executable without its icons.

Rift image generation used the built-in ImageGen tool with a transparent background. Prompt: recreate the recognizable curved, pointed purple/pink vortex silhouette as a centered standalone high-resolution game UI cutout with equal padding, lilac highlights and subtle crystalline texture. Keep its center and gaps transparent; remove scenery, black shadow plate, text and watermarks. Preserve readability at 64 px; no baked bloom, outer ring or notification panel. The renderer adds the adjustable glow and gradient.
