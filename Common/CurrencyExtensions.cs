namespace dm.TanTipBot.Common
{
    public static class CurrencyExtensions
    {
        public static uint GetColor(this Currency currency)
        {
            switch (currency)
            {
                case Currency.Tangram:
                    return Color.TANGRAM_BLUE;
            }
            return 0x0;
        }

        public static string GetIcon(this Currency currency)
        {
            switch (currency)
            {
                case Currency.Tangram:
                    return Asset.TANGRAM;
            }
            return string.Empty;
        }
    }
}
