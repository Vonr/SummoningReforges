namespace SummoningReforges.Prefix
{
    public sealed class LoyalPrefix : BasePrefix
    {
        public override StaffType StaffType => StaffType.Minion;

        public override float Value => 1.6f;
        public override float TagDamage => 4f / 3f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 0.85f;
        }
    }
}
