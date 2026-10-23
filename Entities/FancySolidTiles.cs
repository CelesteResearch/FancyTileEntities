// SPDX-License-Identifier: MPL-2.0

using Celeste.Mods.FancyTileEntities.Runtime.Tiles;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.LevelLoader.Terrain;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FancyTileEntities.Tiles;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FancyTileEntities.Entities
{
    [CustomEntity("FancyTileEntities/FancySolidTiles")]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.FancyTileEntities.FancySolidTiles")]
    [EverestTrackedAs(typeof(OriginalSolidTiles))]
    public sealed class FancySolidTiles : OriginalSolidTiles
    {
        private readonly VirtualMap<char> tileMap;
        private readonly bool blendEdges;
        private readonly int seed;

        public static FancySolidTiles Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new FancySolidTiles(level, data, offset, TileMapCodec.GenerateTileMap(data.Attr("tileData")));

        private FancySolidTiles(OriginalLevelSceneRoot level, EntityData data, Vector2 offset, VirtualMap<char> map)
            : base(data.Position + offset, map, level.SolidWorldFactory.Create(data.Position + offset, true))
        {
            tileMap = map;
            blendEdges = data.Bool("blendEdges");
            seed = data.Int("randomSeed");
            if (!data.Bool("loadGlobally"))
                RemoveTag(((IOriginalGameTagsEnvironment)SourceWorld.EngineContext).Tags.Global.Value);
            Remove(Tiles);
            Remove(AnimatedTiles);
            for (int x = 0; x < map.Columns; x++)
            for (int y = 0; y < map.Rows; y++)
                if (map.AnyInSegmentAtTile(x, y) && map[x, y] != '0')
                    Add(new LightOccludeState(new IntRect(x * 8, y * 8, 8, 8)));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var random = ((ISourceRandomProcessEnvironment)SourceWorld.EngineContext).RandomProcess;
            random.PushRandom(seed != 0 ? seed : random.Random.Next());
            AutotileGeneration generated;
            try
            {
                var tiler = ((ISourceAutotilingResources)SourceWorld.EngineContext).ForegroundAutotiler;
                var level = (OriginalLevelSceneRoot)scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                generated = blendEdges
                    ? FancyMixedOverlay.Generate(tiler, tileMap, (int)X / 8 - bounds.Left,
                        (int)Y / 8 - bounds.Top, level.SolidsData, default(AutotileBehavior))
                    : tiler.GenerateMap(tileMap, default(AutotileBehavior));
            }
            finally
            {
                random.PopRandom();
            }

            Tiles = generated.TileGrid;
            Tiles.VisualExtend = 1;
            Add(Tiles);
            Add(AnimatedTiles = generated.SpriteOverlay);
        }
    }
}
