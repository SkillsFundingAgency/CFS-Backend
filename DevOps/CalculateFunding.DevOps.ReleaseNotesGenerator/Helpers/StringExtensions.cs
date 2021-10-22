namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Helpers
{
    public static class StringExtensions
    {
        public static string EncodeURL(this string url)
            => url.Replace(" ", "%20");
    }
}
