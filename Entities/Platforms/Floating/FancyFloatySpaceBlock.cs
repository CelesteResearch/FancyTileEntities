// SPDX-License-Identifier: MPL-2.0

using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision.Riding;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.Floating
{
    [CustomEntity("FancyTileEntities/FancyFloatySpaceBlock")]
    [EverestTrackedAs(typeof(FloatySpaceBlockState))]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyFloatySpaceBlock")]
    public class FancyFloatySpaceBlock : FloatySpaceBlockState
    {
        private readonly VirtualMap<char> tileMap;
        private readonly LightOccludeState badLightOcclude;
        private int seed;
        internal VirtualMap<char> TileMap => tileMap;
        internal int TileSeed => seed;

        public static FancyFloatySpaceBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancyFloatySpaceBlock(level, data, offset);

        public FancyFloatySpaceBlock(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height,
                EntityAttributeReader.Char(data.Values, "connectsTo", '3'), data.Bool("disableSpawnOffset"),
                level.SolidWorldFactory.Create(data.Position + offset, true))
        {
            seed = data.Int("randomSeed");
            badLightOcclude = Get<LightOccludeState>();
            var engine = level.EngineContext;
            var random = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
            random.PushRandom(seed != 0 ? seed : ((ISourceRandomEnvironment)engine).Random.Next());
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            random.PopRandom();
            var colliders = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
            Collider = colliders;
            foreach (HitboxColliderGeometry hitbox in colliders.colliders)
                Add(new LightOccludeState(new IntRect((int)hitbox.Position.X, (int)hitbox.Position.Y,
                    (int)hitbox.Width, (int)hitbox.Height)));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Remove(badLightOcclude);
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
    }
}
