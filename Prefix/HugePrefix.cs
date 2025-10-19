namespace SummoningReforges.Prefix
{
    public class HugePrefix : BasePrefix
    {
        public override float Value => 1.6f;
        public override float ArmorPenetration => 0.06f;
        public override float Speed => 2f / 3f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 4f / 3f;
            scaleMult *= 1.5f;
        }
    }
}
