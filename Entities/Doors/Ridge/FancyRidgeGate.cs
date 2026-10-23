// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Doors.Ridge;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;

namespace Celeste.Mods.FancyTileEntities.Entities.Doors.Ridge
{
    [CustomEntity("FancyTileEntities/FancyRidgeGate")]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyRidgeGate")]
    public sealed class FancyRidgeGate : OriginalRidgeGateState
    {
        internal readonly string Flag;
        internal Celeste.Core.Scheduling.ISourceEngineContext EngineContext => engine;

        public static FancyRidgeGate Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level, data, offset);

        private FancyRidgeGate(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(level.EngineContext, data.Position + offset, data.Width, data.Height, data.FirstNodeNullable(offset),
                "objects/ridgeGate", level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            Flag = data.Attr("flag", "");
            Remove(Get<SpriteImage>());
            var tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData", ""));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
            Add(((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).ForegroundAutotiler
                .GenerateMap(tileMap, default(AutotileBehavior)).TileGrid);
        }
    }
}
