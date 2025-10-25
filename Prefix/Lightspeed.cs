namespace SummoningReforges.Prefix
{
    public sealed class LightspeedPrefix : BasePrefix
    {
        public override float Value => 1.6f;
        public override float Speed => 1.5f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 0.65f;
        }
    }
}
