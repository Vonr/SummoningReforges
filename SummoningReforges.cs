using System;
using MonoMod.Cil;
using SummoningReforges.Prefix;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SummoningReforges
{
    public sealed class SummoningReforgesSystem : ModSystem
    {
        public override void Load()
        {
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += static (orig, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2) =>
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
                    Modifiers? mods = null;
                    var scale = 1.0f;

                    if (parentProj != null)
                    {
                        if (SummoningReforges.GetModifiers(parentProj, out var foundMods))
                        {
                            mods = foundMods;
                        }
                        var defaultParent = new Projectile();
                        defaultParent.SetDefaults(parentProj.type);
                        scale = parentProj.scale / defaultParent.scale;
                    }
                    else if (spawnSource is EntitySource_ItemUse itemUse)
                    {
                        var item = itemUse.Item;
                        var defaultItem = new Item();
                        defaultItem.SetDefaults(item.type);

                        if (SummoningReforges.GetModifiers(item, out var foundMods))
                        {
                            mods = foundMods;
                        }
                        scale = item.scale / defaultItem.scale;
                    }

                    spawned.scale *= scale;
                    if (mods is Modifiers m)
                    {
                        SummoningReforges.SetModifiers(spawned, m);
                        spawned.minionSlots *= m.SummonCost;
                    }
                }

                return idx;
            };

            IL_Projectile.Damage += static il =>
            {
                var c = new ILCursor(il);

                var field = typeof(ProjectileID.Sets).GetField(nameof(ProjectileID.Sets.SummonTagDamageMultiplier));
                if (field == null || !c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find ldsfld for {field}");
                }

                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdelemR4()))
                {
                    throw new InvalidProgramException($"Couldn't find ldelem.r4 after {field}");
                }

                c.EmitLdarg0();
                c.EmitDelegate<Func<float, Projectile, float>>((orig, self) =>
                {
                    if (SummoningReforges.GetModifiers(self, out var mods))
                    {
                        orig += mods.TagDamage - 1f;
                    }

                    return orig;
                });
            };

            IL_Player.FreeUpPetsAndMinions += static il =>
            {
                var c = new ILCursor(il);

                var field = typeof(ItemID.Sets).GetField(nameof(ItemID.Sets.StaffMinionSlotsRequired));
                if (field == null || !c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find ldsfld for {field}");
                }

                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdelemR4()))
                {
                    throw new InvalidProgramException($"Couldn't find ldelem.r4 after {field}");
                }

                c.EmitLdarg1();
                c.EmitDelegate<Func<float, Item, float>>((orig, item) =>
                {
                    if (SummoningReforges.GetModifiers(item, out var mods))
                    {
                        orig *= mods.SummonCost;
                    }

                    return orig;
                });
            };

            IL_Projectile.Update += static il =>
            {
                var c = new ILCursor(il);

                var field = typeof(Projectile).GetField(nameof(Projectile.extraUpdates));
                if (field == null || !c.TryGotoNext(MoveType.After, i => i.MatchLdfld(field)))
                {
                    throw new InvalidProgramException($"Couldn't find {field}");
                }

                c.EmitLdarg0();
                c.EmitDelegate<Func<int, Projectile, int>>((extraUpdates, self) =>
                {
                    if (SummoningReforges.GetData(self, out var data))
                    {
                        data.PartialUpdates += self.MaxUpdates * (data.Modifiers.Speed - 1.0f);
                        var fullUpdates = (int)data.PartialUpdates;
                        data.PartialUpdates -= fullUpdates;
                        return extraUpdates + fullUpdates;
                    }

                    return extraUpdates;
                });
            };
        }
    }

    public sealed class SummoningReforges : Mod
    {
        public static bool GetData(Projectile projectile, out SummoningReforgesProjectileData data)
        {
            if (projectile?.IsMinionOrSentryRelated == true && projectile.TryGetGlobalProjectile(out data))
            {
                return true;
            }

            data = new();
            return false;
        }

        public static bool GetModifiers(Item item, out Modifiers mods)
        {
            if (item != null && PrefixLoader.GetPrefix(item.prefix) is BasePrefix basePrefix)
            {
                mods = new(armorPenetration: basePrefix.ArmorPenetration, speed: basePrefix.Speed, summonCost: basePrefix.SummonCost, tagDamage: basePrefix.TagDamage);
                return true;
            }

            mods = new();
            return false;
        }

        public static bool GetModifiers(Projectile projectile, out Modifiers mods)
        {
            if (GetData(projectile, out var data))
            {
                mods = data.Modifiers;
                return true;
            }

            mods = new();
            return false;
        }

        public static bool GetModifiers(int projectileIndex, out Modifiers mods)
        {
            return GetModifiers(Main.projectile?[projectileIndex], out mods);
        }

        public static void SetModifiers(Projectile projectile, Modifiers from)
        {
            if (GetData(projectile, out var data))
            {
                data.Modifiers = from;
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

    public sealed class SummoningReforgesProjectileData : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public Modifiers Modifiers { get; set; }
        public float PartialUpdates { get; set; }

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.IsMinionOrSentryRelated;
        }
    }

    public sealed class SummoningReforgesGlobalItem : GlobalItem
    {
        public override bool AllowPrefix(Item item, int pre)
        {
            return SummoningReforges.IsSummonStaff(item)
                ? pre < 0 || PrefixLoader.GetPrefix(pre) != null
                : base.AllowPrefix(item, pre);
        }
    }

    public sealed class SummoningReforgesGlobalNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (SummoningReforges.GetModifiers(projectile, out var mods))
            {
                modifiers.ScalingArmorPenetration += mods.ArmorPenetration;
            }

            base.ModifyHitByProjectile(npc, projectile, ref modifiers);
        }
    }

    public readonly struct Modifiers(float armorPenetration = 0f, float speed = 1f, float summonCost = 1f, float tagDamage = 1f)
    {
        public float ArmorPenetration { get; } = armorPenetration;
        public float Speed { get; } = speed;
        public float SummonCost { get; } = summonCost;
        public float TagDamage { get; } = tagDamage;
    }
}
