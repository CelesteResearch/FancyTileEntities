// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Mechanisms.RumbleWalls;
using Celeste.Mods.FancyTileEntities.Entities.Walls.Rumbling;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FancyTileEntities.Runtime.Walls
{
    public sealed class FancyRumbleWallHooks : IDisposable
    {
        private static readonly object lifecycleLock = new object();
        private static readonly HashSet<ISourceEngineContext> owners = new HashSet<ISourceEngineContext>();
        private static Hook hook;
        private readonly ISourceEngineContext engine;
        private bool disposed;

        public FancyRumbleWallHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            lock (lifecycleLock)
            {
                if (!owners.Add(engine))
                    throw new InvalidOperationException("Fancy rumble hooks already installed for this engine.");
                try
                {
                    if (hook == null)
                        hook = new Hook(typeof(OriginalCrumbleWallOnRumbleState).GetMethod("Break"),
                            (Action<Action<OriginalCrumbleWallOnRumbleState>, OriginalCrumbleWallOnRumbleState>)
                            OnBreak);
                }
                catch
                {
                    owners.Remove(engine);
                    throw;
                }
            }
        }

        private static void OnBreak(Action<OriginalCrumbleWallOnRumbleState> orig,
            OriginalCrumbleWallOnRumbleState self)
        {
            if (self is FancyCrumbleWallOnRumble fancy && owners.Contains(fancy.EngineContext)) fancy.BreakTiles();
            else orig(self);
        }

        public void Dispose()
        {
            lock (lifecycleLock)
            {
                if (disposed) return;
                disposed = true;
                owners.Remove(engine);
                if (owners.Count == 0)
                {
                    hook?.Dispose();
                    hook = null;
                }
            }
        }
    }
}
