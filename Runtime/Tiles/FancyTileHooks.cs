// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Tiles.Autotiling;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Game.Presentation.Platforms.FloatySpaceBlocks;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Graphics;
using Celeste.Mods.FancyTileEntities.Entities.Platforms.Floating;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Celeste.Mods.FancyTileEntities.Runtime.Tiles
{
    public sealed class FancyTileHooks : IDisposable
    {
        [ThreadStatic] internal static FancyOverlayScope CurrentOverlay;
        private readonly ISourceEngineContext engine;
        private static readonly List<IDisposable> hooks = new();
        private static readonly ConditionalWeakTable<ISourceEngineContext, FancyTileHooks> owners = new();
        private static readonly object lifecycle = new();
        private static int ownerCount;
        internal bool Disposed;

        internal static FancyTileHooks FindOwner(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var owner) ? owner : null;

        private delegate TileGrid GroupGenerate(List<FloatySpaceBlockState> group, IntPoint min, IntPoint max,
            ref char type, ISourceEngineContext owner);

        private delegate bool OverlayCheck(Autotiler tiler, object terrain, int x, int y, ref IntRect fill);

        public FancyTileHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            lock (lifecycle)
            {
                owners.Add(engine, this);
                ownerCount++;
                if (ownerCount != 1) return;
                try
                {
                    InstallHooks();
                }
                catch
                {
                    owners.Remove(engine);
                    ownerCount--;
                    Disposed = true;
                    DisposeHooks();
                    throw;
                }
            }
        }

        private static void InstallHooks()
        {
            InstallCheck(typeof(Autotiler));
            InstallCheck(typeof(EverestAutotiler));
            hooks.Add(new ILHook(typeof(FloatySpaceBlockState).GetMethod(nameof(FloatySpaceBlockState.Awake)), il =>
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    i => i.Operand is Mono.Cecil.MethodReference method &&
                         method.Name == nameof(OriginalFloatyBlockGroupTiles.Generate) &&
                         method.DeclaringType.FullName == typeof(OriginalFloatyBlockGroupTiles).FullName);
                c.Remove();
                c.EmitDelegate<GroupGenerate>(GenerateOwnedGroup);
            }));
        }

        private static TileGrid GenerateOwnedGroup(List<FloatySpaceBlockState> group, IntPoint min, IntPoint max,
            ref char type, ISourceEngineContext engine)
        {
            var owner = FindOwner(engine);
            return owner != null
                ? owner.GenerateGroup(group, min, max, ref type, engine)
                : OriginalFloatyBlockGroupTiles.Generate(group, min, max, ref type, engine);
        }

        private static void InstallCheck(Type type)
        {
            var check = type.GetMethod("CheckTile",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var terrainType = check.GetParameters()[0].ParameterType;
            var ignoreMethod = terrainType.GetMethod("Ignore");
            var ignoreBody = new DynamicMethod("FancyIgnore", typeof(bool), new[] { typeof(object), typeof(char) },
                type, true);
            var emit = ignoreBody.GetILGenerator();
            emit.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            emit.Emit(System.Reflection.Emit.OpCodes.Castclass, terrainType);
            emit.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            emit.Emit(System.Reflection.Emit.OpCodes.Call, ignoreMethod);
            emit.Emit(System.Reflection.Emit.OpCodes.Ret);
            var ignore = (Func<object, char, bool>)ignoreBody.CreateDelegate(typeof(Func<object, char, bool>));
            var emptyMethod = type.GetMethod("IsEmpty",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var emptyBody = new DynamicMethod("FancyEmpty", typeof(bool), new[] { typeof(Autotiler), typeof(char) },
                type, true);
            emit = emptyBody.GetILGenerator();
            emit.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            emit.Emit(System.Reflection.Emit.OpCodes.Castclass, type);
            emit.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            emit.Emit(System.Reflection.Emit.OpCodes.Call, emptyMethod);
            emit.Emit(System.Reflection.Emit.OpCodes.Ret);
            var empty = (Func<Autotiler, char, bool>)emptyBody.CreateDelegate(typeof(Func<Autotiler, char, bool>));
            OverlayCheck callback = (Autotiler tiler, object terrain, int x, int y, ref IntRect fill) =>
            {
                var scope = CurrentOverlay;
                if (scope == null || !ReferenceEquals(scope.Tiler, tiler) ||
                    scope.Owner == null || scope.Owner.Disposed) return false;
                char overlay = scope.Tiles.SafeCheck(x - fill.Left, y - fill.Top);
                if (empty(tiler, overlay)) fill = IntRect.Empty;
                else if (ignore(terrain, overlay)) return true;
                return false;
            };
            hooks.Add(new ILHook(check, il =>
            {
                var c = new ILCursor(il);
                var proceed = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_3);
                c.Emit(OpCodes.Ldarg_S, (byte)4);
                c.Emit(OpCodes.Ldarga_S, (byte)5);
                c.EmitDelegate(callback);
                c.Emit(OpCodes.Brfalse, proceed);
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(proceed);
            }));
        }

        private TileGrid GenerateGroup(List<FloatySpaceBlockState> group, IntPoint min, IntPoint max, ref char type,
            ISourceEngineContext owner)
        {
            if (!ReferenceEquals(owner, engine))
                return OriginalFloatyBlockGroupTiles.Generate(group, min, max, ref type, owner);
            var rect = new IntRect(min.X / 8, min.Y / 8, (max.X - min.X) / 8 + 1, (max.Y - min.Y) / 8 + 1);
            var map = new VirtualMap<char>(rect.Width, rect.Height, '0');
            foreach (var block in group)
            {
                int x = (int)(block.X / 8f) - rect.X, y = (int)(block.Y / 8f) - rect.Y;
                int w = (int)(block.Width / 8f), h = (int)(block.Height / 8f);
                for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    map[i, j] = type;
            }

            foreach (var block in group)
                if (block is FancyFloatySpaceBlock fancy)
                {
                    int x = (int)(block.X / 8f) - rect.X, y = (int)(block.Y / 8f) - rect.Y;
                    var source = fancy.TileMap;
                    if (map.Rows < source.Rows - y || map.Columns < source.Columns - x)
                        throw new IndexOutOfRangeException(
                            "Destination not large enough to hold contents of source. \n Set `blindCopy` to true to disregard this message.");
                    for (int i = 0; i < source.Columns; i++)
                    for (int j = 0; j < source.Rows; j++)
                        map[x + i, y + j] = source[i, j];
                }

            var master = group.Count == 1 ? group[0] as FancyFloatySpaceBlock : null;
            if (master != null && master.TileSeed != 0)
                ((ISourceRandomProcessEnvironment)engine).RandomProcess.PushRandom(master.TileSeed);
            var generated =
                ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler.GenerateMap(map,
                    new AutotileBehavior
                        { EdgesExtend = false, EdgesIgnoreOutOfLevel = false, PaddingIgnoreOutOfLevel = false });
            if (master != null && master.TileSeed != 0)
                ((ISourceRandomProcessEnvironment)engine).RandomProcess.PopRandom();
            return generated.TileGrid;
        }

        private static void DisposeHooks()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }

        public void Dispose()
        {
            lock (lifecycle)
            {
                if (Disposed) return;
                Disposed = true;
                owners.Remove(engine);
                if (--ownerCount == 0) DisposeHooks();
            }
        }
    }
}
