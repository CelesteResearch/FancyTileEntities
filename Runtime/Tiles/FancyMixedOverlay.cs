// SPDX-License-Identifier: MPL-2.0

using System;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;

namespace Celeste.Mods.FancyTileEntities.Runtime.Tiles
{
    public static class FancyMixedOverlay
    {
        public static AutotileGeneration Generate(Autotiler tiler, VirtualMap<char> forcedTiles, int startX, int startY,
            VirtualMap<char> mapData)
            => Generate(tiler, forcedTiles, startX, startY, mapData,
                new AutotileBehavior
                    { EdgesExtend = true, EdgesIgnoreOutOfLevel = true, PaddingIgnoreOutOfLevel = true });

        public static AutotileGeneration Generate(Autotiler tiler, VirtualMap<char> forcedTiles, int startX, int startY,
            VirtualMap<char> mapData, AutotileBehavior behavior)
        {
            var tileGrid = new TileGrid(tiler.TileWidth, tiler.TileHeight, forcedTiles.Columns, forcedTiles.Rows);
            var animatedTiles = new AnimatedTiles(forcedTiles.Columns, forcedTiles.Rows, tiler.AnimatedTilesBank,
                tiler.EngineContext);
            var forceFill = new IntRect(startX, startY, forcedTiles.Columns, forcedTiles.Rows);
            var prior = FancyTileHooks.CurrentOverlay;
            FancyTileHooks.CurrentOverlay = new FancyOverlayScope(tiler, forcedTiles);
            try
            {
                for (int i = startX; i < startX + forcedTiles.Columns; i += 50)
                for (int j = startY; j < startY + forcedTiles.Rows; j += 50)
                {
                    if (!mapData.AnyInSegmentAtTile(i, j))
                    {
                        j = j / 50 * 50;
                        continue;
                    }

                    for (int x = i; x < Math.Min(i + 50, startX + forcedTiles.Columns); x++)
                    for (int y = j; y < Math.Min(j + 50, startY + forcedTiles.Rows); y++)
                    {
                        var candidates = tiler.QueryTile(mapData, x, y, forceFill, forcedTiles[x - startX, y - startY],
                            behavior);
                        if (!candidates.HasCandidates) continue;
                        var random = ((ISourceRandomEnvironment)tiler.EngineContext).Random;
                        tileGrid.Tiles[x - startX, y - startY] = random.Choose(candidates.Textures);
                        if (candidates.HasOverlays)
                            animatedTiles.Set(x - startX, y - startY, random.Choose(candidates.OverlapSprites));
                    }
                }
            }
            finally
            {
                FancyTileHooks.CurrentOverlay = prior;
            }

            return new AutotileGeneration { TileGrid = tileGrid, SpriteOverlay = animatedTiles };
        }
    }
}
