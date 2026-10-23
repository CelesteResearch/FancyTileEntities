// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using Celeste.Mods.FancyTileEntities.Runtime.Doors.Ridge;
using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using Celeste.Mods.FancyTileEntities.Runtime.Platforms.IntroCrushers;
using Celeste.Mods.FancyTileEntities.Runtime.Walls;
using Celeste.Core.Scheduling;
using Celeste.Mods.FancyTileEntities.Runtime.Platforms.Boss;
using Celeste.Mods.FancyTileEntities.Gameplay.Platforms.Falling;
using Celeste.Everest.Modules.Runtime;

namespace Celeste.Mods.FancyTileEntities.Modules
{
    public sealed class FancyTileEntitiesModule : SourceEverestModule
    {
        public FancyTileEntitiesModule(ISourceEngineContext engine) : base(engine)
        {
        }

        private FancyFinalBossMovingBlockHooks finalBossMovingBlockHooks;
        private FancyFallingBlockHooks fallingBlockHooks;
        private BetterIntroCrusherHooks introCrusherHooks;
        private FancyTileHooks tileHooks;
        private FancyRumbleWallHooks rumbleWallHooks;
        private FancyRidgeGateHooks ridgeGateHooks;

        public override void Load()
        {
            finalBossMovingBlockHooks = new FancyFinalBossMovingBlockHooks(SourceEngine);
            rumbleWallHooks = new FancyRumbleWallHooks(SourceEngine);
            introCrusherHooks = new BetterIntroCrusherHooks(SourceEngine);
            fallingBlockHooks = new FancyFallingBlockHooks(SourceEngine);
            tileHooks = new FancyTileHooks(SourceEngine);
            ridgeGateHooks = new FancyRidgeGateHooks(SourceEngine);
        }

        public override void Unload()
        {
            finalBossMovingBlockHooks?.Dispose();
            finalBossMovingBlockHooks = null;
            tileHooks?.Dispose();
            tileHooks = null;
            rumbleWallHooks?.Dispose();
            rumbleWallHooks = null;
            fallingBlockHooks?.Dispose();
            fallingBlockHooks = null;
            introCrusherHooks?.Dispose();
            introCrusherHooks = null;
            ridgeGateHooks?.Dispose();
            ridgeGateHooks = null;
        }
    }
}
