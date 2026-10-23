// SPDX-License-Identifier: MPL-2.0

using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Walls.Breakable;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FancyTileEntities.Entities
{
    [CustomEntity("FancyTileEntities/FancyDashBlock")]
    [EverestTrackedAs(typeof(OriginalDashBlockState))]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyDashBlock")]
    public sealed class FancyDashBlock : OriginalDashBlockState
    {
        private readonly VirtualMap<char> tileMap;
        private readonly bool blendIn;
        private readonly bool permanent;

        public static FancyDashBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancyDashBlock(level, room, data, offset);

        private FancyDashBlock(OriginalLevelSceneRoot level, OriginalLevelRoomData room, OriginalLevelEntityData data,
            Vector2 offset)
            : base(data.Position + offset, '3', data.Width, data.Height, data.Bool("blendin"),
                data.Bool("permanent", true), data.Bool("canDash", true),
                level.CreateEntityId(room, data), level.SolidWorldFactory)
        {
            blendIn = data.Bool("blendin");
            permanent = data.Bool("permanent", true);
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
        }

        public override void Awake(object scene)
        {
            AwakeSolid(scene);
            var tiler = ((ISourceAutotilingResources)SourceWorld.EngineContext).ForegroundAutotiler;
            TileGrid tileGrid;
            if (!blendIn)
            {
                tileGrid = tiler.GenerateMap(tileMap, default(AutotileBehavior)).TileGrid;
                for (int x = 0; x < tileMap.Columns; x++)
                for (int y = 0; y < tileMap.Rows; y++)
                    if (tileMap.AnyInSegmentAtTile(x, y) && tileMap[x, y] != '0')
                        Add(new LightOccludeState(new IntRect(x * 8, y * 8, 8, 8)));
            }
            else
            {
                var level = (OriginalLevelSceneRoot)Scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                tileGrid = FancyMixedOverlay.Generate(tiler, tileMap, (int)(X / 8f) - bounds.Left,
                    (int)(Y / 8f) - bounds.Top, level.SolidsData).TileGrid;
                Add(new EffectCutoutState());
                Depth = -10501;
            }

            Add(tileGrid);
            Add(new TileInterceptorState(tileGrid, true));
            if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                    typeof(RoomPlayerSession).FullName, this)) RemoveSelf();
        }

        public override void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true)
        {
            if (playSound)
                ((ISourceSpatialAudioEnvironment)SourceWorld.EngineContext).SpatialAudio.Play(
                    "event:/game/general/wall_break_stone", Position);
            for (int x = 0; x < Width / 8f; x++)
            for (int y = 0; y < Height / 8f; y++)
                if (tileMap.AnyInSegmentAtTile(x, y) && tileMap[x, y] != '0')
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(SourceWorld.EngineContext.EntityPools
                        .Create<DebrisState>().Init(Position + new Vector2(4 + x * 8, 4 + y * 8),
                            tileMap[x, y], playDebrisSound).BlastFrom(from));
            Collidable = false;
            if (permanent) RemoveAndFlagAsGone();
            else RemoveSelf();
        }

        public override int GetLandSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetStepSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetWallSoundIndex(ISourceEntityCollider player, int side) =>
            TileSurfaceSounds.Wall(SourceWorld.EngineContext, this, player, side, tileMap);
    }
}
