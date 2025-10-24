using System;
using MonoMod.Cil;
using SummoningReforges.Prefix;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SummoningReforges
{
    public class SummoningReforgesSystem : ModSystem
    {
        public override void Load()
        {
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += static (On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float orig, IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2) =>
            {
                Projectile parentProj = null;
                if (spawnSource is EntitySource_Parent parent && parent.Entity is Projectile proj && proj.IsMinionOrSentryRelated)
                {
                    parentProj = proj;
                }

                var idx = orig.Invoke(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2);
                var spawned = Main.projectile[idx];
                if (spawned.IsMinionOrSentryRelated)
                {
                    Modifiers mods = new();
                    var scale = 1.0f;

                    if (parentProj != null)
                    {
                        mods = SummoningReforges.GetModifiers(parentProj);
                    }
                    else if (spawnSource is EntitySource_ItemUse itemUse)
                    {
                        var item = itemUse.Item;
                        var defaultItem = new Item();
                        defaultItem.SetDefaults(item.type);

                        mods = SummoningReforges.GetModifiers(item);
                        scale = item.scale / defaultItem.scale;
                    }

                    SummoningReforges.CopyModifiers(spawned, mods);
                    spawned.minionSlots *= mods.SummonCost;
                    spawned.scale *= scale;
                }

                return idx;
            };

            On_Projectile.Kill += static (On_Projectile.orig_Kill orig, Projectile self) =>
            {
                SummoningReforges.SetModifiers(self, null);
                orig.Invoke(self);
            };

            IL_Projectile.Damage += static (ILContext il) =>
            {
                var c = new ILCursor(il);

                var field = typeof(ProjectileID.Sets).GetField(nameof(ProjectileID.Sets.SummonTagDamageMultiplier));
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find {field}");
                }

                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdelemR4()))
                {
                    throw new InvalidProgramException($"Couldn't find ldelem.r4 after {field}");
                }

                c.EmitLdarg0();
                c.EmitDelegate<Func<float, Projectile, float>>((orig, self) =>
                {
                    var mods = SummoningReforges.GetModifiers(self);
                    return orig * mods.TagDamage;
                });
            };

            IL_Player.FreeUpPetsAndMinions += static (ILContext il) =>
            {
                var c = new ILCursor(il);

                var field = typeof(ItemID.Sets).GetField(nameof(ItemID.Sets.StaffMinionSlotsRequired));
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find {field}");
                }

                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdelemR4()))
                {
                    throw new InvalidProgramException($"Couldn't find ldelem.r4 after {field}");
                }

                c.EmitLdarg1();
                c.EmitDelegate<Func<float, Item, float>>((orig, item) =>
                {
                    var mods = SummoningReforges.GetModifiers(item);
                    return orig * mods.SummonCost;
                });
            };

            IL_Projectile.Update += static (ILContext il) =>
            {
                var c = new ILCursor(il);

                var field = typeof(Projectile).GetField(nameof(Projectile.extraUpdates));
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find {field}");
                }

                c.EmitLdarg0();
                c.EmitDelegate<Func<int, Projectile, int>>((extraUpdates, self) =>
                {
                    if (self.TryGetGlobalProjectile<SummoningReforgesProjectileData>(out var g))
                    {
                        g.PartialUpdates += self.MaxUpdates * (g.Modifiers.Speed - 1.0f);
                        var fullUpdates = (int)g.PartialUpdates;
                        g.PartialUpdates -= fullUpdates;
                        return extraUpdates + fullUpdates;
                    }

                    return extraUpdates;
                });
            };
        }
    }

    public class SummoningReforges : Mod
    {
        public static Modifiers GetModifiers(Item item)
        {
            return item != null && PrefixLoader.GetPrefix(item.prefix) is BasePrefix basePrefix
                ? new()
                {
                    ArmorPenetration = basePrefix.ArmorPenetration,
                    Speed = basePrefix.Speed,
                    SummonCost = basePrefix.SummonCost,
                    TagDamage = basePrefix.TagDamage,
                }
                : new();
        }

        public static Modifiers GetModifiers(Projectile projectile)
        {
            if (projectile == null)
            {
                return new();
            }

#pragma warning disable IDE0046
            if (projectile.TryGetGlobalProjectile<SummoningReforgesProjectileData>(out var g))
            {
                return g.Modifiers;
            }
#pragma warning restore IDE0046

            return new();
        }

        public static Modifiers GetModifiers(int projectileIndex)
        {
            return GetModifiers(Main.projectile?[projectileIndex]);
        }

        public static void CopyModifiers(Projectile projectile, Modifiers from)
        {
            CopyModifiers(projectile.whoAmI, from);
        }

        public static void CopyModifiers(int projectileIndex, Modifiers from)
        {
            SetModifiers(projectileIndex, new()
            {
                ArmorPenetration = from.ArmorPenetration,
                Speed = from.Speed,
                SummonCost = from.SummonCost,
                TagDamage = from.TagDamage,
            });
        }

        public static void SetModifiers(Projectile projectile, Modifiers from)
        {
            if (projectile.TryGetGlobalProjectile<SummoningReforgesProjectileData>(out var g))
            {
                g.Modifiers = from;
            }
        }

        public static void SetModifiers(int projectileIndex, Modifiers from)
        {
            SetModifiers(Main.projectile?[projectileIndex], from);
        }

        private static bool ItemProjectileIs(Item item, Predicate<Projectile> pred)
        {
            if (item.shoot != ProjectileID.None)
            {
                var proj = new Projectile();
                proj.SetDefaults(item.shoot);
                return pred.Invoke(proj);
            }

            return false;
        }

        public static bool IsSummonStaff(Item item)
        {
            return ItemProjectileIs(item, static proj => proj.minion || proj.sentry);
        }

        public static bool IsMinionStaff(Item item)
        {
            return ItemProjectileIs(item, static proj => proj.minion);
        }

        public static bool IsSentryStaff(Item item)
        {
            return ItemProjectileIs(item, static proj => proj.sentry);
        }
    }

    public class SummoningReforgesProjectileData : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public Modifiers Modifiers { get; set; } = new();
        public float PartialUpdates { get; set; }

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.IsMinionOrSentryRelated;
        }
    }

    public class SummoningReforgesGlobalItem : GlobalItem
    {
        public override bool AllowPrefix(Item item, int pre)
        {
            return SummoningReforges.IsSummonStaff(item)
                ? pre < 0 || PrefixLoader.GetPrefix(pre) != null
                : base.AllowPrefix(item, pre);
        }
    }

    public class SummoningReforgesGlobalNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.IsMinionOrSentryRelated)
            {
                modifiers.ScalingArmorPenetration += SummoningReforges.GetModifiers(projectile).ArmorPenetration;
            }
            base.ModifyHitByProjectile(npc, projectile, ref modifiers);
        }
    }

    public class Modifiers
    {
        public float ArmorPenetration { get; set; }
        public float Speed { get; set; } = 1.0f;
        public float SummonCost { get; set; } = 1.0f;
        public float TagDamage { get; set; } = 1.0f;
    }
}
