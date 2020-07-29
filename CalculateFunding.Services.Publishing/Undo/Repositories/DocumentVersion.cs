using System;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Publishing.Undo.Repositories
{
    public class DocumentVersion
    {
        private DocumentVersion(int major,
            int minor)
        {
            Major = major;
            Minor = minor;
            DecimalValue = Convert.ToDecimal($"{major}.{minor}");
        }

        public int Major { get; }
        
        public int Minor { get; }
        
        public decimal DecimalValue { get; }

        public static implicit operator DocumentVersion(string versionLiteral)
        {
            Guard.IsNullOrWhiteSpace(versionLiteral, nameof(versionLiteral));
            
            string[] versionParts = versionLiteral.Split(".");
            
            Guard.IsNotEmpty(versionParts, nameof(versionParts));
            Guard.Ensure(versionParts.Length == 2, "Version should be in major.minor format");  
                
            return new DocumentVersion(versionParts[0].AsInt(),
                versionParts[1].AsInt());
        }
    }
}