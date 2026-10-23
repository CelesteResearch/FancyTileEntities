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
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Gameplay.Walls.Concealment;

namespace Celeste.Mods.FancyTileEntities.Entities.Walls.Concealment
{
    [CustomEntity("FancyTileEntities/FancyFakeWall")]
    [EverestTrackedAs(typeof(FakeWallState))]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyFakeWall")]
    public sealed class FancyFakeWall : FakeWallState
    {
        private readonly VirtualMap<char> tileMap;

        public static FancyFakeWall Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancyFakeWall(level, room, data, offset);

        private FancyFakeWall(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            OriginalLevelEntityData data, Vector2 offset)
            : base(level.CreateEntityId(room, data), data.Position + offset, data.Char("tiletype", '3'),
                data.Width, data.Height, data.Enum("type", FakeWallMode.Wall),
                new SecretWallContext
                {
                    PlaySound = (path, position) => ((IOriginalGameplayProcess)level.EngineContext).AudioSession.Play(
                        path, position, ((IOriginalGameplayProcess)level.EngineContext).SaveContext)
                }, level.EngineContext)
        {
            PlayTransitionReveal = data.Bool("playTransitionReveal");
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Remove(Tiles);
            Remove(Get<TileInterceptorState>());
            var level = (OriginalLevelSceneRoot)Scene;
            var tiler = ((ISourceAutotilingResources)level.EngineContext).ForegroundAutotiler;
            if (Mode == FakeWallMode.Wall)
            {
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                int x = (int)X / 8 - bounds.Left;
                int y = (int)Y / 8 - bounds.Top;
                Tiles = FancyMixedOverlay.Generate(tiler, tileMap, x, y, level.SolidsData).TileGrid;
            }
            else Tiles = tiler.GenerateMap(tileMap, default(AutotileBehavior)).TileGrid;

            Add(Tiles);
            Add(new TileInterceptorState(Tiles, false));
        }
    }
}
