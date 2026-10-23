// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Core.Collections;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.FancyTileEntities.Tiles
{
    public static class TileMapCodec
    {
        public static ColliderListGeometry GenerateBetterColliderGrid(VirtualMap<char> tileMap, int cellWidth,
            int cellHeight)
        {
            ColliderListGeometry colliders = new ColliderListGeometry();
            List<HitboxColliderGeometry> prevCollidersOnX = new List<HitboxColliderGeometry>();
            HitboxColliderGeometry prevCollider = null;

            void ExtendOrAdd()
            {
                bool extendedOnX = false;
                foreach (HitboxColliderGeometry hitbox in prevCollidersOnX)
                {
                    if (hitbox.Position.X + hitbox.Width == prevCollider.Position.X &&
                        hitbox.Position.Y == prevCollider.Position.Y &&
                        hitbox.Height == prevCollider.Height)
                    {
                        // Weird check, but hey.
                        extendedOnX = true;
                        hitbox.Width += cellWidth;
                        prevCollider = null;
                        break;
                    }
                }

                if (!extendedOnX)
                {
                    colliders.Add(prevCollider);
                    prevCollidersOnX.Add(prevCollider);
                    prevCollider = null;
                }
            }

            for (int x = 0; x < tileMap.Columns; x++)
            {
                for (int y = 0; y < tileMap.Rows; y++)
                {
                    if (tileMap.AnyInSegmentAtTile(x, y) && tileMap[x, y] != '0')
                    {
                        if (prevCollider == null)
                            prevCollider =
                                new HitboxColliderGeometry(cellWidth, cellHeight, x * cellWidth, y * cellHeight);
                        else
                            prevCollider.Height += cellHeight;
                    }
                    else if (prevCollider != null)
                    {
                        ExtendOrAdd();
                    }
                }

                if (prevCollider != null)
                {
                    ExtendOrAdd();
                }
            }

            return colliders.colliders.Length > 0 ? colliders : null;
        }

        public static VirtualMap<char> GenerateTileMap(string tileMap)
        {
            if (string.IsNullOrWhiteSpace(tileMap))
                throw new ArgumentException("Attempted to generate a TileMap with no tiles in it!");

            // Backwards compatibility, tileMap strings previously used `,` as the row separator
            char delim = tileMap.Contains(',') ? ',' : '\n';

            string[] tileStrings = tileMap.Split(delim);
            tileStrings = Array.ConvertAll(tileStrings, s => s.Trim());

            int columns = tileStrings.Max(s => s.Length);
            int rows = tileStrings.Length;

            tileStrings = Array.ConvertAll(tileStrings, s =>
            {
                while (s.Length < columns)
                {
                    s += '0';
                }

                return s;
            });

            char[,] tileData = new char[columns, rows];
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    tileData[x, y] = tileStrings[y][x];
                }
            }

            return new VirtualMap<char>(tileData);
        }
    }
}
