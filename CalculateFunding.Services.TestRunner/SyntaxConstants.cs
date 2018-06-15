using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.TestRunner
{
    public static class SyntaxConstants
    {
       

        /// <summary>
        /// 
        /// </summary>
        public const string providerExpression =            @"(the)(\s)+(provider)(\s)+(.*)+(\s)+'(.*)'";

        /// <summary>
        /// 
        /// </summary>
        public const string assertCalcExpression =          @"(the)(\s)+(result)(\s)+(for)(\s)+'([^'.]*)'(\s)+(.*)(\s)+'([^'.]*)'";

        /// <summary>
        /// the result for 'Calc 1' is equal to dataset 'Test DS' field 'Field Name'
        /// </summary>
        public const string assertCalcDatasetExpression =   @"(the)(\s)+(result)(\s)+(for)(\s)+'(.*)'(\s)+(.*)(\s)+(the)(\s)+(dataset)(\s)+'(.*)'(\s)+(field)(\s)+'(.*)'";

        /// <summary>
        /// 
        /// </summary>
        public const string datasetSourceField =            @"(the)(\s)+(result)(\s)+(for)(\s)+'([^'.]*)'(\s)+(.*)(\s)+(the)(\s)+(dataset)(\s)+'([^'.]*)'(\s)+(field)(\s)+'([^'.]*)'";

        /// <summary>
        /// Datasets parse regex
        /// </summary>
        public const string SourceDatasetStep =             @"(the)(\s)+(dataset)(\s)+'(.*)'(\s)+(field)(\s)+'(([^'.]*))'(\s)(([^'.]*)(\s))'(([^'.]*))'";

    }
}
