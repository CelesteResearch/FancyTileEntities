// SPDX-License-Identifier: MPL-2.0

using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Audio;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Mechanisms.RumbleWalls;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using Celeste.Mods.FancyTileEntities.Runtime.Walls;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FancyTileEntities.Entities.Walls.Rumbling
{
    [CustomEntity("FancyTileEntities/FancyCrumbleWallOnRumble")]
    [EverestTrackedAs(typeof(OriginalCrumbleWallOnRumbleState))]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyCrumbleWallOnRumble")]
    public class FancyCrumbleWallOnRumble : OriginalCrumbleWallOnRumbleState
    {
        internal Celeste.Core.Scheduling.ISourceEngineContext EngineContext => SourceWorld.EngineContext;
        private readonly VirtualMap<char> tileMap;

        public static FancyCrumbleWallOnRumble Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new FancyCrumbleWallOnRumble(level, room, data, offset);

        private FancyCrumbleWallOnRumble(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, data.Char("tiletype", 'm'), data.Width, data.Height, data.Bool("blendin"),
                data.Bool("persistent"), level.CreateEntityId(room, data),
                level.SolidWorldFactory.Create(data.Position + offset, true))
        {
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            var colliders = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
            foreach (HitboxColliderGeometry hitbox in colliders.colliders)
                Add(new LightOccludeState(new IntRect((int)hitbox.Position.X, (int)hitbox.Position.Y, (int)hitbox.Width,
                    (int)hitbox.Height)));
            Collider = colliders;
        }

        public override void Awake(object scene)
        {
            FancyWallLifecycle.AwakeSolid(this, scene);
            var tiler = ((ISourceAutotilingResources)SourceWorld.EngineContext).ForegroundAutotiler;
            TileGrid tiles;
            if (!FancyWallLifecycle.GetBlendIn(this))
                tiles = tiler.GenerateMap(tileMap, default(AutotileBehavior)).TileGrid;
            else
            {
                var level = (OriginalLevelSceneRoot)Scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                tiles = FancyMixedOverlay.Generate(tiler, tileMap, (int)(X / 8f) - bounds.Left,
                    (int)(Y / 8f) - bounds.Top, level.SolidsData).TileGrid;
                Depth = -10501;
            }

            Add(tiles);
            Add(new TileInterceptorState(tiles, true));
            if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                    typeof(RoomPlayerSession).FullName, this)) RemoveSelf();
        }

        internal void BreakTiles()
        {
            if (!Collidable || Scene == null) return;
            var engine = SourceWorld.EngineContext;
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/new_content/game/10_farewell/quake_rockbreak", Position);
            Collidable = false;
            for (int x = 0; x < Width / 8f; x++)
            for (int y = 0; y < Height / 8f; y++)
                if (tileMap[x, y] != '0' && tileMap[x, y] != '\0' && !SourceSceneCollision.CheckRect(
                        ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        new IntRect((int)X + x * 8, (int)Y + y * 8, 8, 8)))
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                        .Init(Position + new Vector2(4 + x * 8, 4 + y * 8), tileMap[x, y], true).BlastFrom(TopCenter));
            if (FancyWallLifecycle.GetPermanent(this))
                ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(FancyWallLifecycle.GetId(this));
            RemoveSelf();
        }

        public override int GetLandSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetStepSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetWallSoundIndex(ISourceEntityCollider player, int side) =>
            TileSurfaceSounds.Wall(SourceWorld.EngineContext, this, player, side, tileMap);
    }
}
