// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections;
using System.Reflection;
using Celeste.Core.Components.Timing;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.IntroCrusher;
using Celeste.Mods.FancyTileEntities.Entities.Platforms.IntroCrushers;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FancyTileEntities.Runtime.Platforms.IntroCrushers
{
    public sealed class BetterIntroCrusherHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook sequence;

        public BetterIntroCrusherHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            sequence = new Hook(
                typeof(OriginalIntroCrusherState).GetMethod("Sequence",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                (Func<Func<OriginalIntroCrusherState, IEnumerator>, OriginalIntroCrusherState, IEnumerator>)Sequence);
        }

        private IEnumerator Sequence(Func<OriginalIntroCrusherState, IEnumerator> original,
            OriginalIntroCrusherState self)
        {
            if (self is BetterIntroCrusher crusher && ReferenceEquals(crusher.EngineContext, engine))
                yield return new SourceSwapImmediately(crusher.RunSequence());
            else yield return new SourceSwapImmediately(original(self));
        }

        public void Dispose() => sequence.Dispose();
    }
}
