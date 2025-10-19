namespace SummoningReforges.Prefix
{
    public class TinyPrefix : BasePrefix
    {
        public override float Value => 1.6f;
        public override float Speed => 1.2f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 0.9f;
            scaleMult *= 0.7f;
        }
    }
}
