// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Doors.Ridge;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FancyTileEntities.Entities.Doors.Ridge;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FancyTileEntities.Runtime.Doors.Ridge
{
    public sealed class FancyRidgeGateHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook hook;

        public FancyRidgeGateHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new Hook(
                typeof(OriginalRidgeGateState).GetMethod("EnterSequence",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Func<Func<OriginalRidgeGateState, Vector2, IEnumerator>, OriginalRidgeGateState, Vector2, IEnumerator>)
                Enter);
        }

        private IEnumerator Enter(Func<OriginalRidgeGateState, Vector2, IEnumerator> original,
            OriginalRidgeGateState self, Vector2 target)
        {
            if (self is FancyRidgeGate gate && ReferenceEquals(gate.EngineContext, engine) &&
                !string.IsNullOrEmpty(gate.Flag) &&
                !((OriginalLevelSceneRoot)self.Scene).Session.GetFlag(gate.Flag)) yield break;
            var routine = original(self, target);
            while (routine.MoveNext()) yield return routine.Current;
        }

        public void Dispose() => hook.Dispose();
    }
}
