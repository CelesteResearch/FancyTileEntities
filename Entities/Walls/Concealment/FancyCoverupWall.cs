// SPDX-License-Identifier: MPL-2.0

using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Game.Entities.Walls.Concealment;

namespace Celeste.Mods.FancyTileEntities.Entities.Walls.Concealment
{
    [CustomEntity("FancyTileEntities/FancyCoverupWall")]
    [EverestTrackedAs(typeof(OriginalCoverupWallState))]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyCoverupWall")]
    public sealed class FancyCoverupWall : OriginalCoverupWallState
    {
        private readonly bool blendIn;
        private readonly VirtualMap<char> tileMap;
        private readonly ISourceEngineContext engine;

        public static FancyCoverupWall Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancyCoverupWall(level.EngineContext, data, offset);

        private FancyCoverupWall(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset)
            : base(engine, data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height)
        {
            this.engine = engine;
            blendIn = data.Bool("blendIn", true);
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
        }

        public override void Added(object scene)
        {
            AddedEntity(scene);
            var tiler = ((ISourceAutotilingResources)engine).ForegroundAutotiler;
            if (blendIn)
            {
                var level = (OriginalLevelSceneRoot)Scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                int x = (int)(X / 8f) - bounds.Left;
                int y = (int)(Y / 8f) - bounds.Top;
                SourceTiles = FancyMixedOverlay.Generate(tiler, tileMap, x, y, level.SolidsData).TileGrid;
            }
            else SourceTiles = tiler.GenerateMap(tileMap, default(AutotileBehavior)).TileGrid;

            Add(SourceTiles);
            Add(new TileInterceptorState(SourceTiles, false));
        }
    }
}
