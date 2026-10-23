// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using System;
using System.Collections.Generic;
using Celeste.Physics.Collision.Riding;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Components;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Randomness;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FinalBossMovingBlock;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Gameplay.Actors.Debris;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.Boss
{
    [CustomEntity("FancyTileEntities/FancyFinalBossMovingBlock")]
    [EverestTrackedAs(typeof(OriginalFinalBossMovingBlockState))]
    [RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancyFinalBossMovingBlock")]
    public sealed partial class FancyFinalBossMovingBlock : OriginalFinalBossMovingBlockState
    {
        private static readonly Func<OriginalFinalBossMovingBlockState, TileGrid>
            GetSprite = Getter<TileGrid>("sprite");

        private static readonly Func<OriginalFinalBossMovingBlockState, TileGrid> GetHighlight =
            Getter<TileGrid>("highlight");

        private static readonly Action<OriginalFinalBossMovingBlockState, TileGrid> SetSprite =
            Setter<TileGrid>("sprite");

        private static readonly Action<OriginalFinalBossMovingBlockState, TileGrid> SetHighlight =
            Setter<TileGrid>("highlight");

        private static readonly Func<OriginalFinalBossMovingBlockState, bool> Highlighted =
            Getter<bool>("isHighlighted");

        private readonly Func<DebrisState> createDebris;
        private readonly LightOccludeState badLightOcclude;
        private readonly ColliderListGeometry normalCollider, highlightCollider;
        private bool wasHighlighted;
        internal ISourceEngineContext EngineContext => Engine;

        public static FancyFinalBossMovingBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level, data, offset);

        private FancyFinalBossMovingBlock(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(data.NodesWithPosition(offset), data.Width, data.Height, data.Int("nodeIndex", 0),
                level.EngineContext, level.SolidWorldFactory)
        {
            createDebris = () => new DebrisState(Engine, SourceWorld.TargetMotionPolicy);
            Remove(GetSprite(this));
            Remove(GetHighlight(this));
            Remove(Get<TileInterceptorState>());
            badLightOcclude = Get<LightOccludeState>();
            var tiles = TileMapCodec.GenerateTileMap(data.Attr("tileData", ""));
            var highlighted = TileMapCodec.GenerateTileMap(data.Attr("tileDataHighlight", ""));
            var random = (ISourceRandomEnvironment)Engine;
            var randomProcess = ((ISourceRandomProcessEnvironment)Engine).RandomProcess;
            var tiler = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)Engine).GameGraphics)
                .ForegroundAutotiler;
            int seed = random.Random.Next();
            randomProcess.PushRandom(seed);
            var sprite = tiler.GenerateMap(tiles, default(AutotileBehavior)).TileGrid;
            SetSprite(this, sprite);
            Add(sprite);
            randomProcess.PopRandom();
            randomProcess.PushRandom(seed);
            var highlight = tiler.GenerateMap(highlighted, default(AutotileBehavior)).TileGrid;
            highlight.Alpha = 0;
            SetHighlight(this, highlight);
            Add(highlight);
            randomProcess.PopRandom();
            Add(new TileInterceptorState(sprite, false));
            highlightCollider = TileMapCodec.GenerateBetterColliderGrid(highlighted, 8, 8);
            normalCollider = TileMapCodec.GenerateBetterColliderGrid(tiles, 8, 8);
            AddOcclusion(normalCollider);
            Collider = normalCollider;
        }

        private static Func<OriginalFinalBossMovingBlockState, T> Getter<T>(string name)
        {
            var dm = new DynamicMethod("GetBoss_" + name, typeof(T),
                new[] { typeof(OriginalFinalBossMovingBlockState) }, typeof(FancyFinalBossMovingBlock), true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(OriginalFinalBossMovingBlockState).GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Func<OriginalFinalBossMovingBlockState, T>)dm.CreateDelegate(
                typeof(Func<OriginalFinalBossMovingBlockState, T>));
        }

        private static Action<OriginalFinalBossMovingBlockState, T> Setter<T>(string name)
        {
            var dm = new DynamicMethod("SetBoss_" + name, typeof(void),
                new[] { typeof(OriginalFinalBossMovingBlockState), typeof(T) }, typeof(FancyFinalBossMovingBlock),
                true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld,
                typeof(OriginalFinalBossMovingBlockState).GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Action<OriginalFinalBossMovingBlockState, T>)dm.CreateDelegate(
                typeof(Action<OriginalFinalBossMovingBlockState, T>));
        }

        private void AddOcclusion(ColliderListGeometry list)
        {
            foreach (var value in list.colliders)
            {
                var box = (HitboxColliderGeometry)value;
                Add(new LightOccludeState(new IntRect((int)box.Position.X, (int)box.Position.Y, (int)box.Width,
                    (int)box.Height)));
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Remove(badLightOcclude);
        }

        public override void Update()
        {
            base.Update();
            if (wasHighlighted || !Highlighted(this)) return;
            Collider = highlightCollider;
            var components = new List<LightOccludeState>(OwnedComponents.GetAll<LightOccludeState>());
            foreach (var component in components) Remove(component);
            AddOcclusion(highlightCollider);
            wasHighlighted = true;
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
