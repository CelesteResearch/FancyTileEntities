// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using Celeste.Core.Collections;
using Celeste.Graphics.Content.Tiles.Autotiling;

namespace Celeste.Mods.FancyTileEntities.Runtime.Tiles
{
    internal sealed class FancyOverlayScope(Autotiler tiler, VirtualMap<char> tiles)
    {
        public readonly Autotiler Tiler = tiler;
        public readonly FancyTileHooks Owner = FancyTileHooks.FindOwner(tiler.EngineContext);
        public readonly VirtualMap<char> Tiles = tiles;
    }
}
