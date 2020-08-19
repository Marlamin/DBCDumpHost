namespace WoWTools.SpellDescParser
{
    public interface ISupplier
    {
        double? SupplyEffectPoint(int spellID, uint? effectIndex);
        int? SupplyDuration(int spellID, uint? effectIndex);
        double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex);
    }
}
