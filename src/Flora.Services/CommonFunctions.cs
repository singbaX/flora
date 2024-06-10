namespace Flora.Services
{
    using AiCodo;
    public static class CommonFunctions
    {
        public static string ToMd5(this string input)
        {
            return input.EncryptMd5_32();
        }
    }
}
