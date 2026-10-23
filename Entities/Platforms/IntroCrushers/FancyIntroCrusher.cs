// SPDX-License-Identifier: MPL-2.0

using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.IntroCrushers
{
    [CustomEntity("FancyTileEntities/FancyIntroCrusher")]
    public sealed class FancyIntroCrusher : BetterIntroCrusher
    {
        private readonly VirtualMap<char> tileMap;

        public new static FancyIntroCrusher Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new FancyIntroCrusher(level, data, offset);

        private FancyIntroCrusher(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(level, Modify(data), offset)
        {
            Remove(tilegrid);
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            Collider = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
            tilegrid = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler.GenerateMap(tileMap, default(AutotileBehavior)).TileGrid;
            Add(tilegrid);
        }

        private static OriginalLevelEntityData Modify(OriginalLevelEntityData data)
        {
            data.Values["tiletype"] = "3";
            return data;
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
            TileSurfaceSounds.Land(engine, this, entity, tileMap);

        public override int GetWallSoundIndex(ISourceEntityCollider player, int side) =>
            TileSurfaceSounds.Wall(engine, this, player, side, tileMap);

        public override int GetStepSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(engine, this, entity, tileMap);
    }
}
