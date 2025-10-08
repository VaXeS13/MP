using System;

namespace MP.Domain.Items
{
    public static class BarcodeHelper
    {
        private const string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string GenerateBarcodeFromGuid(Guid guid)
        {
            var bytes = guid.ToByteArray();
            var number = Math.Abs(BitConverter.ToInt64(bytes, 0));

            if (number == 0)
                return "0".PadLeft(13, '0');

            var result = string.Empty;
            while (number > 0)
            {
                result = Base36Chars[(int)(number % 36)] + result;
                number /= 36;
            }

            return result.PadLeft(13, '0');
        }

        public static bool IsValidBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            if (barcode.Length < 10 || barcode.Length > 15)
                return false;

            foreach (var ch in barcode)
            {
                if (!Base36Chars.Contains(ch))
                    return false;
            }

            return true;
        }
    }
}
