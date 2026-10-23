// SPDX-License-Identifier: MPL-2.0

using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Everest.Entities.Triggers.Entities;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.Falling
{
    [CustomEntity("FancyTileEntities/FancyFallingBlock")]
    [EverestTrackedAs(typeof(OriginalFallingBlockState))]
    public sealed class FancyFallingBlock : OriginalFallingBlockState
    {
        internal readonly bool ManualTrigger;
        internal Celeste.Core.Scheduling.ISourceEngineContext EngineContext => engine;
        private readonly VirtualMap<char> tileMap;
        private readonly AnimatedTiles animatedTiles;
        private readonly LightOccludeState badLightOcclude;

        public static FancyFallingBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancyFallingBlock(level, data, offset);

        private FancyFallingBlock(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, '3', data.Width, data.Height, data.Bool("finalBoss"),
                data.Bool("behind"), data.Bool("climbFall", true),
                level.SolidWorldFactory.Create(data.Position + offset, false), level.EngineContext)
        {
            Remove(SourceTiles);
            Remove(Get<TileInterceptorState>());
            badLightOcclude = Get<LightOccludeState>();
            var randomProcess = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
            int newSeed = ((ISourceRandomEnvironment)engine).Random.Next();
            randomProcess.PushRandom(newSeed);
            tileMap = TileMapCodec.GenerateTileMap(data.Attr("tileData"));
            var tiler = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler;
            var generated = tiler.GenerateMap(tileMap, default(AutotileBehavior));
            SourceTiles = generated.TileGrid;
            Add(SourceTiles);
            Add(animatedTiles = generated.SpriteOverlay);
            randomProcess.PopRandom();
            if (data.Bool("finalBoss"))
            {
                var highlighted = TileMapCodec.GenerateTileMap(data.Attr("tileDataHighlight"));
                randomProcess.PushRandom(newSeed);
                var highlight = tiler.GenerateMap(highlighted, default(AutotileBehavior)).TileGrid;
                highlight.Alpha = 0f;
                SourceHighlight = highlight;
                Add(SourceHighlight);
                randomProcess.PopRandom();
            }

            var colliders = TileMapCodec.GenerateBetterColliderGrid(tileMap, 8, 8);
            foreach (var collider in colliders.colliders)
            {
                var hitbox = (HitboxColliderGeometry)collider;
                Add(new LightOccludeState(new IntRect((int)hitbox.Position.X, (int)hitbox.Position.Y,
                    (int)hitbox.Width, (int)hitbox.Height)));
            }

            Collider = colliders;
            Add(new TileInterceptorState(SourceTiles, false));
            ManualTrigger = data.Bool("manualTrigger");
            if (ManualTrigger) Add(new EntityTriggerListenerState(() => Triggered = true, () => Triggered = true));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Remove(badLightOcclude);
        }

        public override void OnShake(Vector2 amount)
        {
            base.OnShake(amount);
            animatedTiles.Position += amount;
        }

        protected override void PushActorV(ISourcePlatformActor actor, int move, float top, float bottom)
        {
            Collidable = false;
            actor.MoveVExact(move, actor.SquishCallback, this);
            actor.LiftSpeed = LiftSpeed;
            Collidable = true;
        }

        protected override void EmitFallParticlesCore()
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var process = (IOriginalGameplayProcess)engine;
            for (int x = 2; x <= Width; x += 4)
            for (int y = 2; y <= Height; y += 4)
            {
                if (!Collider.Collide(new Vector2(X + x, Y + y))) continue;
                if (SourceSceneCollision.FirstPoint<ISourceEntityCollider>(
                        ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", new Vector2(X + x, Y + y - 3)) !=
                    null)
                    level.Particles.Emit(process.ParticleTypes.FallingBlock.P_FallDustA, 2,
                        new Vector2(X + x, Y + y), Vector2.One * 4f, (float)System.Math.PI / 2f);
                level.Particles.Emit(process.ParticleTypes.FallingBlock.P_FallDustB, 2,
                    new Vector2(X + x, Y + y), Vector2.One * 4f);
                break;
            }
        }

        internal void EmitMixedLandParticles()
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var process = (IOriginalGameplayProcess)engine;
            for (int x = 2; x <= Width; x += 4)
            for (int y = (int)Height - 2; y >= 0; y -= 4)
            {
                if (!Collider.Collide(new Vector2(X + x, Y + y))) continue;
                if (SourceSceneCollision.FirstPoint<ISourceEntityCollider>(
                        ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", new Vector2(X + x, Y + y + 3)) !=
                    null)
                {
                    level.ParticlesFG.Emit(process.ParticleTypes.FallingBlock.P_FallDustA, 1,
                        new Vector2(X + x, Y + y), Vector2.One * 4f, -(float)System.Math.PI / 2f);
                    float direction = x < Width / 2f ? (float)System.Math.PI : 0f;
                    level.ParticlesFG.Emit(process.ParticleTypes.FallingBlock.P_LandDust, 1,
                        new Vector2(X + x, Y + y), Vector2.One * 4f, direction);
                }

                break;
            }
        }

        public override int GetLandSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(engine, this, entity, tileMap);

        public override int GetWallSoundIndex(ISourceEntityCollider player, int side) =>
            TileSurfaceSounds.Wall(engine, this, player, side, tileMap);

        public override int GetStepSoundIndex(ISourceEntityCollider entity) =>
            TileSurfaceSounds.Land(engine, this, entity, tileMap);
    }
}
