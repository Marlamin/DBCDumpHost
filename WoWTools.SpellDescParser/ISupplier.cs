namespace WoWTools.SpellDescParser
{
    public interface ISupplier
    {
        double? SupplyEffectPoint(int spellID, uint? effectIndex);
        int? SupplyDuration(int spellID, uint? effectIndex);
        double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex);
        int? SupplyMaxStacks(int spellID);
        int? SupplyAuraPeriod(int spellID, uint? effectIndex);
        int? SupplyProcCharges(int spellID);
        int? SupplyProcChance(int spellID);
        int? SupplyChainTargets(int spellID, uint? effectIndex);
        int? SupplyMaxTargetLevel(int spellID);
        int? SupplyMaxTargets(int spellID);
        int? SupplyMinRange(int spellID);
        int? SupplyMaxRange(int spellID);
        int? SupplyEffectAmplitude(int spellID, uint? effectIndex);
        int? SupplyEffectMisc(int spellID);
        string? SupplySpellName(int spellID);
    }
}
