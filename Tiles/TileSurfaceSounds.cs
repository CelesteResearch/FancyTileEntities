// SPDX-License-Identifier: MPL-2.0

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Collections;
using Celeste.Game.Execution.Audio.Surfaces;
using Celeste.Game.Execution.Levels;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Game.Audio.Surfaces;

namespace Celeste.Mods.FancyTileEntities.Tiles
{
    public static class TileSurfaceSounds
    {
        public static int Land(ISourceEngineContext engine, SourceSolidEntityState solid, ISourceEntityCollider entity,
            VirtualMap<char> tileMap)
        {
            var anchors = (ISourceEntitySurfaceAnchors)entity;
            int value = At(engine, solid, anchors.BottomCenter + Vector2.UnitY * 4f, tileMap);
            if (value == -1) value = At(engine, solid, anchors.BottomLeft + Vector2.UnitY * 4f, tileMap);
            if (value == -1) value = At(engine, solid, anchors.BottomRight + Vector2.UnitY * 4f, tileMap);
            return value;
        }

        public static int Wall(ISourceEngineContext engine, SourceSolidEntityState solid, ISourceEntityCollider player,
            int side, VirtualMap<char> tileMap)
        {
            var anchors = (ISourceEntitySurfaceAnchors)player;
            int value = At(engine, solid, anchors.Center + Vector2.UnitX * side * 8f, tileMap);
            if (value < 0) value = At(engine, solid, anchors.Center + new Vector2(side * 8, -6f), tileMap);
            if (value < 0) value = At(engine, solid, anchors.Center + new Vector2(side * 8, 6f), tileMap);
            return value;
        }

        public static int At(ISourceEngineContext engine, SourceSolidEntityState solid, Vector2 position,
            VirtualMap<char> tileMap)
        {
            int x = (int)((position.X - solid.X) / 8f), y = (int)((position.Y - solid.Y) / 8f);
            if (x >= 0 && y >= 0 && x < tileMap.Columns && y < tileMap.Rows)
            {
                char tile = tileMap[x, y];
                if (tile == 'k')
                    return ((OriginalLevelSceneRoot)solid.Scene).CoreMode switch
                    {
                        RoomCoreMode.Hot => OriginalSurfaceIndex.CoreMoltenRock,
                        RoomCoreMode.Cold => OriginalSurfaceIndex.CoreIce,
                        _ => OriginalSurfaceIndex.Dirt
                    };
                if (tile != '0' && ((IOriginalTileSurfaceEnvironment)engine)
                    .SurfaceIndices.TileToIndex.TryGetValue(tile, out int index)) return index;
            }

            return -1;
        }
    }
}
