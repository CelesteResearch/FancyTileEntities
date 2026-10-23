// SPDX-License-Identifier: MPL-2.0
// This file is part of the FancyTileEntities port; see LICENSE.

using System;
using System.Numerics;
using Celeste.Core.Randomness;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Platforms.FinalBossMovingBlock;
using Celeste.Game.Gameplay.Environment.Water;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Actors.Debris;

namespace Celeste.Mods.FancyTileEntities.Entities.Platforms.Boss
{
    public sealed partial class FancyFinalBossMovingBlock
    {
        internal static void StopParticles(Action<OriginalFinalBossMovingBlockState, Vector2> orig,
            OriginalFinalBossMovingBlockState self, Vector2 moved)
        {
            if (self is FancyFinalBossMovingBlock block)
            {
                var level = (OriginalLevelSceneRoot)self.Scene;
                float direction = (float)Math.Atan2(moved.Y, moved.X);
                if (moved.X > 0f)
                {
                    Vector2 vector = new Vector2(self.Right - 1f, self.Top);
                    for (int i = 0; (float)i < self.Height; i += 4)
                    {
                        for (int num = (int)self.Width - 2; num >= 0; num -= 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)num, self.Y + (float)i)))
                            {
                                level.Particles.Emit(
                                    ((IOriginalGameplayProcess)((FancyFinalBossMovingBlock)self).Engine).ParticleTypes
                                    .FinalBossMovingBlock.P_Stop,
                                    vector + Vector2.UnitY * (2 + num +
                                                              SourceRandomMath.Range(
                                                                  ((ISourceRandomEnvironment)block.Engine).Random, -1,
                                                                  1)), direction);
                                break;
                            }
                        }
                    }
                }
                else if (moved.X < 0f)
                {
                    Vector2 vector2 = new Vector2(self.Left, self.Top);
                    for (int j = 0; (float)j < self.Height; j += 4)
                    {
                        for (int k = 2; (float)k <= self.Width; k += 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)k, self.Y + (float)j)))
                            {
                                level.Particles.Emit(
                                    ((IOriginalGameplayProcess)((FancyFinalBossMovingBlock)self).Engine).ParticleTypes
                                    .FinalBossMovingBlock.P_Stop,
                                    vector2 + Vector2.UnitY * (2 + j +
                                                               SourceRandomMath.Range(
                                                                   ((ISourceRandomEnvironment)block.Engine).Random, -1,
                                                                   1)), direction);
                                break;
                            }
                        }
                    }
                }

                if (moved.Y > 0f)
                {
                    Vector2 vector3 = new Vector2(self.Left, self.Bottom - 1f);
                    for (int l = 0; (float)l < self.Width; l += 4)
                    {
                        for (int num2 = (int)self.Height - 2; num2 >= 0; num2 -= 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)l, self.Y + (float)num2)))
                            {
                                level.Particles.Emit(
                                    ((IOriginalGameplayProcess)((FancyFinalBossMovingBlock)self).Engine).ParticleTypes
                                    .FinalBossMovingBlock.P_Stop,
                                    vector3 + Vector2.UnitX * (2 + l +
                                                               SourceRandomMath.Range(
                                                                   ((ISourceRandomEnvironment)block.Engine).Random, -1,
                                                                   1)), direction);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (!(moved.Y < 0f))
                    {
                        return;
                    }

                    Vector2 vector4 = new Vector2(self.Left, self.Top);
                    for (int m = 0; (float)m < self.Width; m += 4)
                    {
                        for (int n = 2; (float)n <= self.Height; n += 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)m, self.Y + (float)n)))
                            {
                                level.Particles.Emit(
                                    ((IOriginalGameplayProcess)((FancyFinalBossMovingBlock)self).Engine).ParticleTypes
                                    .FinalBossMovingBlock.P_Stop,
                                    vector4 + Vector2.UnitX * (2 + m +
                                                               SourceRandomMath.Range(
                                                                   ((ISourceRandomEnvironment)block.Engine).Random, -1,
                                                                   1)), direction);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                orig(self, moved);
            }
        }

        private static void BreakParticles(OriginalFinalBossMovingBlockState self)
        {
            Vector2 center = self.Center;
            for (int i = 0; (float)i < self.Width; i += 4)
            {
                for (int j = 0; (float)j < self.Height; j += 4)
                {
                    Vector2 vector = self.Position + new Vector2(2 + i, 2 + j);
                    if (self.Collider.Collide(vector))
                    {
                        ((OriginalLevelSceneRoot)self.Scene).Particles.Emit(
                            ((IOriginalGameplayProcess)((FancyFinalBossMovingBlock)self).Engine).ParticleTypes
                            .FinalBossMovingBlock.P_Break, 1, vector, Vector2.One * 2f,
                            (float)Math.Atan2((vector - center).Y, (vector - center).X));
                    }
                }
            }
        }

        internal static void ImpactParticles(Action<OriginalFinalBossMovingBlockState, Vector2> orig,
            OriginalFinalBossMovingBlockState self, Vector2 moved)
        {
            if (self is FancyFinalBossMovingBlock block)
            {
                if (moved.X < 0f)
                {
                    for (int i = 0; (float)i < self.Height / 8f; i++)
                    {
                        for (int j = 2; (float)j <= self.Width; j += 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)j, self.Y + (float)i)))
                            {
                                Vector2 vector = new Vector2(self.Left + (float)j - 1f, self.Top + 4f + (float)(i * 8));
                                if (!CheckScenePoint<OriginalWaterState>(self, vector) &&
                                    CheckScenePoint<SourceSolidEntityState>(self, vector))
                                {
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector + Vector2.UnitY * 2f + Vector2.UnitY * 2f, 0f);
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector + Vector2.UnitY * 2f - Vector2.UnitY * 2f, 0f);
                                }
                            }
                        }
                    }
                }
                else if (moved.X > 0f)
                {
                    for (int k = 0; (float)k < self.Height / 8f; k++)
                    {
                        for (int num = (int)self.Width - 2; num >= 0; num -= 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)num, self.Y + (float)k)))
                            {
                                Vector2 vector2 = new Vector2(self.Left + (float)num + 1f,
                                    self.Top + 4f + (float)(k * 8));
                                if (!CheckScenePoint<OriginalWaterState>(self, vector2) &&
                                    CheckScenePoint<SourceSolidEntityState>(self, vector2))
                                {
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector2 + Vector2.UnitY * 2f, MathF.PI);
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector2 - Vector2.UnitY * 2f, MathF.PI);
                                }
                            }
                        }
                    }
                }

                if (moved.Y < 0f)
                {
                    for (int l = 0; (float)l < self.Width / 8f; l++)
                    {
                        for (int m = 2; (float)m <= self.Height; m += 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)l, self.Y + (float)m)))
                            {
                                Vector2 vector3 = new Vector2(self.Left + 4f + (float)(l * 8),
                                    self.Top + (float)m - 1f);
                                if (!CheckScenePoint<OriginalWaterState>(self, vector3) &&
                                    CheckScenePoint<SourceSolidEntityState>(self, vector3))
                                {
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector3 + Vector2.UnitX * 2f, MathF.PI / 2f);
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector3 - Vector2.UnitX * 2f, MathF.PI / 2f);
                                }

                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (!(moved.Y > 0f))
                    {
                        return;
                    }

                    for (int n = 0; (float)n < self.Width / 8f; n++)
                    {
                        for (int num2 = (int)self.Height - 2; num2 >= 0; num2 -= 4)
                        {
                            if (self.Collider.Collide(new Vector2(self.X + (float)n, self.Y + (float)num2)))
                            {
                                Vector2 vector4 = new Vector2(self.Left + 4f + (float)(n * 8),
                                    self.Top + (float)num2 + 1f);
                                if (!CheckScenePoint<OriginalWaterState>(self, vector4) &&
                                    CheckScenePoint<SourceSolidEntityState>(self, vector4))
                                {
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector4 + Vector2.UnitX * 2f, -MathF.PI / 2f);
                                    ((OriginalLevelSceneRoot)self.Scene).ParticlesFG.Emit(
                                        ((IOriginalGameplayProcess)block.Engine).ParticleTypes.CrushBlock.P_Impact,
                                        vector4 - Vector2.UnitX * 2f, -MathF.PI / 2f);
                                }

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                orig(self, moved);
            }
        }

        internal static void Finish(Action<OriginalFinalBossMovingBlockState> orig,
            OriginalFinalBossMovingBlockState self)
        {
            if (self is FancyFinalBossMovingBlock block)
            {
                Vector2 vector = self.CenterRight + Vector2.UnitX * 10f;
                for (int i = 0; (float)i < self.Width / 8f; i++)
                {
                    for (int j = 0; (float)j < self.Height / 8f; j++)
                    {
                        ((ISourceScene)self.Scene).SourceScene.QueueSourceEntity(block.Engine.EntityPools
                            .Create<DebrisState>(block.createDebris)
                            .Init(self.Position + new Vector2(4 + i * 8, 4 + j * 8), 'f', true).BlastFrom(vector));
                    }
                }

                BreakParticles(self);
                self.DestroyStaticMovers();
                self.RemoveSelf();
            }
            else
            {
                orig(self);
            }
        }

        private static bool CheckScenePoint<T>(OriginalFinalBossMovingBlockState self, Vector2 at)
        {
            var scene = ((ISourceScene)self.Scene).SourceScene;
            return SourceSceneCollision.CheckPoint(scene.Entities, scene.Catalog.Find(typeof(T)).RegistryKey, at);
        }
    }
}
