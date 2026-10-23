// SPDX-License-Identifier: MPL-2.0

using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Scene.Entities;
using Celeste.Core.Maps;
using Celeste.Game.Entities.Mechanisms.RumbleWalls;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Walls.Concealment;
using Celeste.Graphics.Content.Tiles;

namespace Celeste.Mods.FancyTileEntities.Runtime.Walls
{
    internal static class FancyWallLifecycle
    {
        internal static readonly Action<SceneEntityState, object> AddedEntity =
            Bind<SceneEntityState>(typeof(SceneEntityState).GetMethod("Added"));

        internal static readonly Action<SourceSolidEntityState, object> AwakeSolid =
            Bind<SourceSolidEntityState>(typeof(SourceSolidEntityState).GetMethod("Awake"));

        internal static readonly Action<ExitBlockState, TileGrid> SetExitTiles = BindTiles();
        internal static readonly Func<OriginalCrumbleWallOnRumbleState, bool> GetBlendIn = BindField<bool>("blendIn");

        internal static readonly Func<OriginalCrumbleWallOnRumbleState, bool> GetPermanent =
            BindField<bool>("permanent");

        internal static readonly Func<OriginalCrumbleWallOnRumbleState, MapEntityId> GetId =
            BindField<MapEntityId>("id");

        private static Func<OriginalCrumbleWallOnRumbleState, T> BindField<T>(string name)
        {
            var field = typeof(OriginalCrumbleWallOnRumbleState).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            var dynamic = new DynamicMethod("FancyReadRumble" + name, typeof(T),
                new[] { typeof(OriginalCrumbleWallOnRumbleState) }, typeof(FancyWallLifecycle), true);
            var il = dynamic.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<OriginalCrumbleWallOnRumbleState, T>)dynamic.CreateDelegate(
                typeof(Func<OriginalCrumbleWallOnRumbleState, T>));
        }

        private static Action<T, object> Bind<T>(MethodInfo method)
        {
            var dynamic = new DynamicMethod("FancyWall" + method.Name, typeof(void),
                new[] { typeof(T), typeof(object) }, typeof(FancyWallLifecycle), true);
            var il = dynamic.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return (Action<T, object>)dynamic.CreateDelegate(typeof(Action<T, object>));
        }

        private static Action<ExitBlockState, TileGrid> BindTiles()
        {
            var method = typeof(ExitBlockState).GetProperty("Tiles").GetSetMethod(true);
            var dynamic = new DynamicMethod("FancySetExitTiles", typeof(void),
                new[] { typeof(ExitBlockState), typeof(TileGrid) }, typeof(FancyWallLifecycle), true);
            var il = dynamic.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return (Action<ExitBlockState, TileGrid>)dynamic.CreateDelegate(typeof(Action<ExitBlockState, TileGrid>));
        }
    }
}
