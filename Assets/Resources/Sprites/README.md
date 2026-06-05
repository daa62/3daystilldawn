# Sprite Replacement Guide

Replace these PNG files with the same file names, then press Play in Unity.

- `Backgrounds/Stage.png` controls the stage background.
- `Entities/Player.png` controls the player entity.
- `Entities/Antlion.png` controls the antlion entity.
- `Tiles/White.png` controls the default tile.
- `Tiles/Red.png`, `Tiles/Blue.png`, and `Tiles/Yellow.png` control the demo tile types.

Runtime code can also swap sprites with resource paths:

```csharp
entity.setSprite("Sprites/Entities/Player");
tile.setSprite("Sprites/Tiles/Red");
```
