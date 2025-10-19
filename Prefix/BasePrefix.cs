using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SummoningReforges.Prefix
{
    public abstract class BasePrefix : ModPrefix
    {
        public virtual StaffType StaffType => StaffType.Both;

        public virtual float Value => 1.05f;
        public virtual float ArmorPenetration => 0.0f;
        public virtual float Speed => 1f;
        public virtual float SummonCost => 1f;
        public virtual float TagDamage => 1f;

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public static class Sets
        {
            public static bool[] Unscalable { get; } = ItemID.Sets.Factory.CreateBoolSet(ItemID.StardustDragonStaff, ItemID.AbigailsFlower, ItemID.StormTigerStaff);
            public static bool[] Unacceleratable { get; } = ItemID.Sets.Factory.CreateBoolSet(ItemID.StardustDragonStaff, ItemID.AbigailsFlower, ItemID.StormTigerStaff);
        }

        public override bool CanRoll(Item item)
        {
            if (!StaffType.SatisfiedBy(item) || !base.CanRoll(item))
            {
                return false;
            }

            var dmg = 1f;
            var kb = 1f;
            var spd = 1f;
            var size = 1f;
            var shtspd = 1f;
            var mcst = 1f;
            var crt = 0;
            SetStats(ref dmg, ref kb, ref spd, ref size, ref shtspd, ref mcst, ref crt);

            return (!Sets.Unscalable[item.type] || size == 1f) && (!Sets.Unacceleratable[item.type] || Speed == 1f);
        }

        public override bool AllStatChangesHaveEffectOn(Item item)
        {
            return true;
        }

        public override float RollChance(Item item)
        {
            var v = float.Max(1f, Value);
            return 1f / (v * v);
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = Value;
        }

        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            var minion = StaffType.Minion.SatisfiedBy(item);

            if (ArmorPenetration != 0.0f)
            {
                yield return new TooltipLine(Mod, "PrefixWeaponArmorPenetration", ArmorPenetrationTooltip.Format(ArmorPenetration))
                {
                    IsModifier = true,
                    IsModifierBad = false,
                };
            }

            if (Speed != 1f)
            {
                yield return new TooltipLine(Mod, "PrefixWeaponSpeed", SpeedTooltip.Format(Speed - 1f))
                {
                    IsModifier = true,
                    IsModifierBad = Speed < 1f,
                };
            }

            if (minion && SummonCost != 1f)
            {
                yield return new TooltipLine(Mod, "PrefixWeaponSummonCost", SummonCostTooltip.Format(SummonCost - 1f))
                {
                    IsModifier = true,
                    IsModifierBad = SummonCost > 1f,
                };
            }

            if (minion && TagDamage != 1f)
            {
                yield return new TooltipLine(Mod, "PrefixWeaponTagDamage", TagDamageTooltip.Format(TagDamage - 1f))
                {
                    IsModifier = true,
                    IsModifierBad = TagDamage < 1f,
                };
            }
        }

        public static LocalizedText ArmorPenetrationTooltip { get; private set; }
        public static LocalizedText SpeedTooltip { get; private set; }
        public static LocalizedText SummonCostTooltip { get; private set; }
        public static LocalizedText TagDamageTooltip { get; private set; }

        public override void SetStaticDefaults()
        {
            ArmorPenetrationTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(ArmorPenetrationTooltip)}");
            SummonCostTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(SummonCostTooltip)}");
            SpeedTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(SpeedTooltip)}");
            TagDamageTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(TagDamageTooltip)}");
        }
    }

    public enum StaffType
    {
        Minion,
        Sentry,
        Both
    }

    public static class StaffTypeExtensions
    {
        public static bool SatisfiedBy(this StaffType staffType, Item item)
        {
            return staffType switch
            {
                StaffType.Minion => SummoningReforges.IsMinionStaff(item),
                StaffType.Sentry => SummoningReforges.IsSentryStaff(item),
                StaffType.Both => SummoningReforges.IsSummonStaff(item),
                _ => throw new System.NotImplementedException(),
            };
        }
    }
}
