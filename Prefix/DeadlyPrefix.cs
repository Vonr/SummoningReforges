namespace SummoningReforges.Prefix
{
    public class DeadlyPrefix : BasePrefix
    {
        public override float Value => 1.6f;
        public override float ArmorPenetration => 0.04f;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1.2f;
        }
    }
}
