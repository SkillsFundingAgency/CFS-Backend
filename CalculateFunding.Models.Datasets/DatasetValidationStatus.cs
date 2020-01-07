using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public enum DatasetValidationStatus
    {
        Queued,
        Processing,
        ValidatingExcelWorkbook,
        ValidatingTableResults,
        SavingResults,
        Validated,
        FailedValidation,
        ExceptionThrown,
    }
}
