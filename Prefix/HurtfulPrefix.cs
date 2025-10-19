namespace SummoningReforges.Prefix
{
    public class HurtfulPrefix : BasePrefix
    {
        public override float Value => 1.2f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1.1f;
        }
    }
}
