namespace SummoningReforges.Prefix
{
    public class BouncyPrefix : BasePrefix
    {
        public override float Value => 1.05f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            knockbackMult *= 2.0f;
        }
    }
}
