using System.Linq;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.Extensions
{
    public static class TableExtensions
    {
        public static string[] AsStrings(this Table t)
        {
            return t.Rows.Select(r => r[0]).ToArray();
        }
    }
}
