// SPDX-License-Identifier: MPL-2.0

using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Mods.FancyTileEntities.Entities.Platforms.Falling;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FancyTileEntities.Gameplay.Platforms.Falling
{
    public sealed class FancyFallingBlockHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook playerFallCheck;
        private readonly Hook landParticles;

        public FancyFallingBlockHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                playerFallCheck = new Hook(typeof(OriginalFallingBlockState).GetMethod("PlayerFallCheck",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                    (Func<Func<OriginalFallingBlockState, bool>, OriginalFallingBlockState, bool>)PlayerFallCheck);
                landParticles = new Hook(typeof(OriginalFallingBlockState).GetMethod("LandParticles",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                    (Action<Action<OriginalFallingBlockState>, OriginalFallingBlockState>)LandParticles);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool PlayerFallCheck(Func<OriginalFallingBlockState, bool> orig, OriginalFallingBlockState self)
        {
            if (self is FancyFallingBlock { ManualTrigger: true } fancy
                && ReferenceEquals(fancy.EngineContext, engine)) return false;
            return orig(self);
        }

        private void LandParticles(Action<OriginalFallingBlockState> orig, OriginalFallingBlockState self)
        {
            if (self is FancyFallingBlock fancy && ReferenceEquals(fancy.EngineContext, engine))
                fancy.EmitMixedLandParticles();
            else orig(self);
        }

        public void Dispose()
        {
            landParticles?.Dispose();
            playerFallCheck?.Dispose();
        }
    }
}
