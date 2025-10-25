namespace SummoningReforges.Prefix
{
    public sealed class LargePrefix : BasePrefix
    {
        public override float Value => 1.1f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            scaleMult *= 1.15f;
        }
    }
}
