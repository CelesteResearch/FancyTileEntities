// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Audio;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Shaking;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;
using System.Linq.Expressions;
using System.Reflection;
using Celeste.Everest.Entities.Triggers.Entities;
using Celeste.Everest.Gameplay.Platforms.IntroCrusher;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Gameplay.Platforms.IntroCrusher;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.IntroCrushers
{
    [CustomEntity("FancyTileEntities/BetterIntroCrusher")]
    public class BetterIntroCrusher : EverestIntroCrusherState
    {
        private static readonly Func<OriginalIntroCrusherState, Vector2> GetStart = CreateGetter<Vector2>("start");

        private static readonly Func<OriginalIntroCrusherState, SoundSourceComponentState> GetShakingSound =
            CreateGetter<SoundSourceComponentState>("shakingSfx");

        protected bool manualTrigger;
        protected bool triggered;
        protected float delay;
        protected float speed;
        internal ISourceEngineContext EngineContext => engine;

        public static BetterIntroCrusher Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new BetterIntroCrusher(level, data, offset);

        protected BetterIntroCrusher(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(level.EngineContext, data, offset, level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            manualTrigger = data.Bool("manualTrigger");
            delay = data.Float("delay", 1.2f);
            speed = data.Float("speed", 2f);
            Remove(Get<EntityTriggerListenerState>());
            Add(new EntityTriggerListenerState(Trigger, StartTriggered));
        }

        private static Func<OriginalIntroCrusherState, T> CreateGetter<T>(string name)
        {
            var self = Expression.Parameter(typeof(OriginalIntroCrusherState));
            return Expression.Lambda<Func<OriginalIntroCrusherState, T>>(Expression.Field(self,
                typeof(OriginalIntroCrusherState).GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)), self).Compile();
        }

        public void Trigger()
        {
            if (manualTrigger) triggered = true;
        }

        public void StartTriggered()
        {
            if (!manualTrigger) return;
            triggered = true;
            Position = end;
            Remove(Get<SimulationCoroutine>());
        }

        internal IEnumerator RunSequence()
        {
            RoomPlayerSession entity;
            do
            {
                yield return null;
                entity = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                    typeof(global::Celeste.Simulation.RoomPlayerSession).FullName);
            } while (!triggered && (manualTrigger || entity == null || entity.X < X + 30f || entity.X > Right + 8f));

            var shakingSfx = GetShakingSound(this);
            shakingSfx.Play("event:/game/00_prologue/fallblock_first_shake");
            float time = delay;
            ShakerComponentState shaker =
                new ShakerComponentState(engine, time, removeOnFinish: true, delegate(Vector2 v) { shake = v; });
            if (delay > 0f) OwnedComponents.Add(shaker);
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                RumbleLength.Medium);
            while (time > 0f)
            {
                RoomPlayerSession entity2 =
                    ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                        typeof(global::Celeste.Simulation.RoomPlayerSession).FullName);

                if (!manualTrigger && entity2 != null &&
                    (entity2.X >= base.X + base.Width - 8f || entity2.X < base.X + 28f))
                {
                    shaker.RemoveSelf();
                    break;
                }

                yield return null;
                time -= engine.DeltaTime;
            }

            for (int num = 2; (float)num < base.Width; num += 4)
            {
                Level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_FallDustA, 2,
                    new Vector2(base.X + (float)num, base.Y), SourceVector2Math.Multiply(Vector2.One, 4f),
                    (float)Math.PI / 2f);
                Level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_FallDustB, 2,
                    new Vector2(base.X + (float)num, base.Y), SourceVector2Math.Multiply(Vector2.One, 4f));
            }

            shakingSfx.Param("release", 1f);
            time = 0f;
            do
            {
                yield return null;
                time = ScalarMath.Approach(time, 1f, speed * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                MoveTo(Vector2Interpolation.Lerp(GetStart(this), end,
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeIn(time),
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic));
            } while (time < 1f);

            for (int num2 = 0; (float)num2 <= base.Width; num2 += 4)
            {
                Level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_FallDustA, 1,
                    new Vector2(base.X + (float)num2, base.Bottom), SourceVector2Math.Multiply(Vector2.One, 4f),
                    -(float)Math.PI / 2f);
                float direction = ((!((float)num2 < base.Width / 2f)) ? 0f : ((float)Math.PI));
                Level.ParticlesFG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock.P_LandDust, 1,
                    new Vector2(base.X + (float)num2, base.Bottom), SourceVector2Math.Multiply(Vector2.One, 4f),
                    direction);
            }

            shakingSfx.Stop();
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/00_prologue/fallblock_first_impact",
                Position);
            Level.Shake();
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                RumbleLength.Medium);
            OwnedComponents.Add(new ShakerComponentState(engine, 0.25f, removeOnFinish: true,
                delegate(Vector2 v) { shake = v; }));
        }
    }
}
