// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.FinalBossMovingBlock;
using Celeste.Mods.FancyTileEntities.Entities.Platforms.Boss;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FancyTileEntities.Runtime.Platforms.Boss
{
    public sealed class FancyFinalBossMovingBlockHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook stop, impact, finish;

        public FancyFinalBossMovingBlockHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            stop = new Hook(
                typeof(OriginalFinalBossMovingBlockState).GetMethod("StopParticles",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalFinalBossMovingBlockState, Vector2>, OriginalFinalBossMovingBlockState, Vector2>)
                Stop);
            impact = new Hook(
                typeof(OriginalFinalBossMovingBlockState).GetMethod("ImpactParticles",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalFinalBossMovingBlockState, Vector2>, OriginalFinalBossMovingBlockState, Vector2>)
                Impact);
            finish = new Hook(
                typeof(OriginalFinalBossMovingBlockState).GetMethod("Finish",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalFinalBossMovingBlockState>, OriginalFinalBossMovingBlockState>)Finish);
        }

        private void Stop(Action<OriginalFinalBossMovingBlockState, Vector2> original,
            OriginalFinalBossMovingBlockState self, Vector2 moved)
        {
            if (self is FancyFinalBossMovingBlock block && ReferenceEquals(block.EngineContext, engine))
                FancyFinalBossMovingBlock.StopParticles(original, self, moved);
            else original(self, moved);
        }

        private void Impact(Action<OriginalFinalBossMovingBlockState, Vector2> original,
            OriginalFinalBossMovingBlockState self, Vector2 moved)
        {
            if (self is FancyFinalBossMovingBlock block && ReferenceEquals(block.EngineContext, engine))
                FancyFinalBossMovingBlock.ImpactParticles(original, self, moved);
            else original(self, moved);
        }

        private void Finish(Action<OriginalFinalBossMovingBlockState> original, OriginalFinalBossMovingBlockState self)
        {
            if (self is FancyFinalBossMovingBlock block && ReferenceEquals(block.EngineContext, engine))
                FancyFinalBossMovingBlock.Finish(original, self);
            else original(self);
        }

        public void Dispose()
        {
            finish.Dispose();
            impact.Dispose();
            stop.Dispose();
        }
    }
}
