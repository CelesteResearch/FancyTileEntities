// SPDX-License-Identifier: MPL-2.0

using System;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using Celeste.Mods.FancyTileEntities.Runtime.Walls;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Gameplay.Walls.Concealment;

namespace Celeste.Mods.FancyTileEntities.Entities.Walls.Concealment
{
    [CustomEntity("FancyTileEntities/FancyExitBlock", "FancyTileEntities/FancyConditionBlock=LoadConditionBlock")]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyExitBlock")]
    public class FancyExitBlock : ExitBlockState
    {
        public enum ConditionBlockModes
        {
            Key,
            Button,
            Strawberry
        }

        private readonly VirtualMap<char> tileMap;

        public static FancyExitBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new FancyExitBlock(level, data, offset);

        private FancyExitBlock(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Char("tileType", '3'),
                level.SolidWorldFactory.Create(data.Position + offset, true), new SecretWallContext
                {
                    PlaySound = (path, position) =>
                    {
                        var process = (IOriginalGameplayProcess)level.EngineContext;
                        process.AudioSession.Play(path, position, process.SaveContext);
                    },
                    TileToSurfaceSoundIndex = tile =>
                        ((IOriginalTileSurfaceEnvironment)level.EngineContext).SurfaceIndices.TileToIndex[tile]
                })
        {
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
        }

        public override void Added(object scene)
        {
            FancyWallLifecycle.AddedEntity(this, scene);
            var level = (OriginalLevelSceneRoot)Scene;
            var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
            var tiles = FancyMixedOverlay
                .Generate(
                    ((Celeste.Graphics.Content.Tiles.Autotiling.ISourceAutotilingResources)level.EngineContext)
                    .ForegroundAutotiler, tileMap, (int)(X / 8f) - bounds.Left, (int)(Y / 8f) - bounds.Top,
                    level.SolidsData).TileGrid;
            FancyWallLifecycle.SetExitTiles(this, tiles);
            Add(tiles);
            Add(new TileInterceptorState(tiles, false));
        }

        public static FancyExitBlock LoadConditionBlock(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var mode = data.Enum("condition", ConditionBlockModes.Key);
            var parts = data.Attr("conditionID").Split(':');
            var id = new Celeste.Core.Maps.MapEntityId(parts[0], Convert.ToInt32(parts[1]));
            bool open;
            switch (mode)
            {
                case ConditionBlockModes.Key:
                    open = level.Session.GetFlag(OriginalDashSwitchState.GetFlagName(id)); break;
                case ConditionBlockModes.Button: open = level.Session.DoNotLoad.Contains(id); break;
                case ConditionBlockModes.Strawberry: open = level.Session.Strawberries.Contains(id); break;
                default: throw new Exception("Condition type not supported!");
            }

            return open ? new FancyExitBlock(level, data, offset) : null;
        }

        protected override void PushActorH(ISourcePlatformActor actor, int move, float left, float right)
        {
            Collidable = false;
            actor.MoveHExact(move, actor.SquishCallback, this);
            actor.LiftSpeed = LiftSpeed;
            Collidable = true;
        }

        protected override void PushActorV(ISourcePlatformActor actor, int move, float top, float bottom)
        {
            Collidable = false;
            actor.MoveVExact(move, actor.SquishCallback, this);
            actor.LiftSpeed = LiftSpeed;
            Collidable = true;
        }

        public override int GetLandSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetStepSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(SourceWorld.EngineContext, this, entity, tileMap);

        public override int GetWallSoundIndex(ISourceEntityCollider player, int side) =>
            TileSurfaceSounds.Wall(SourceWorld.EngineContext, this, player, side, tileMap);
    }
}
