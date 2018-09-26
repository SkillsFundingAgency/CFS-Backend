using System;
using System.Text;

namespace CalculateFunding.Services.CodeGeneration
{
    public static class CodeGenerationConstants
    {
        public static String VisualBasicDefaultSourceCode
        {
            get
            {
                var source = new StringBuilder();
                source.AppendLine(@"' --- Providers ---- '");
                source.AppendLine(@"");
                source.AppendLine(@"' Provider fields can be accessed from the 'Provider' property:");
                source.AppendLine(@"' Dim yearOpened = Provider.DateOpened.Year ");
                source.AppendLine(@"");
                source.AppendLine(@"' --- Datasets ---- '");
                source.AppendLine(@"' Referenced dataset fields can be accessed via the 'Datasets' property:");
                source.AppendLine(@"' Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis");
                source.AppendLine(@"");
                source.AppendLine(@"' --- Caclulations ---- '");
                source.AppendLine(@"' Other calculations within the same specification can be referred to directly:");
                source.AppendLine(@"'  Dim rate = P004_PriRate()");
                source.AppendLine(@"");
                source.AppendLine(@"' For backwards compatability legacy Store functions and properties are available");
                source.AppendLine(@" 'LAToProv()");
                source.AppendLine(@"' Exclude()");
                source.AppendLine(@"' Print()");
                source.AppendLine(@"' IIf()");
                source.AppendLine(@"' currentScenario ");
                source.AppendLine(@"' rid ");
                source.AppendLine(@" ");
                source.AppendLine(@"' To exclude a result then Return Exclude()");
                source.AppendLine(@"");
                source.AppendLine(@"Return Decimal.MinValue");

                return source.ToString();
            }
        }
    }
}
