namespace StrongboxRolling.Utils
{
    /// <summary>
    /// Strongbox Type
    /// </summary>
    public enum SBType
    {
        Regular,
        Arcanist,
        Diviner,
        Cartographer,
        Jeweller,
        Artisan,
        Blacksmith,
        Armourer,
        Unique
    }
    public class ItemCodes()
    {
        public static readonly string ScourCode = @"Metadata/Items/Currency/CurrencyConvertToNormal";
        public static readonly string AlchCode = @"Metadata/Items/Currency/CurrencyUpgradeToRare";
        public static readonly string EngineerCode = @"Metadata/Items/Currency/CurrencyStrongboxQuality";
        public static readonly string ChaosCode = @"Metadata/Items/Currency/CurrencyRerollRare";
        public static readonly string AltCode = @"Metadata/Items/Currency/CurrencyRerollMagic";
        public static readonly string TransmuteCode = @"Metadata/Items/Currency/CurrencyUpgradeToMagic";
        public static readonly string AugCode = @"Metadata/Items/Currency/CurrencyAddModToMagic";
        public static readonly string WisdomCode = @"Metadata/Items/Currency/CurrencyIdentification";
        
    }

}
