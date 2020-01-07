using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class DatasetUploadValidationModel
    {
	    public DatasetUploadValidationModel(ExcelPackage excelPackage, Func<IEnumerable<ProviderSummary>> providerSummaries, DatasetDefinition datasetDefinition)
	    {
		    ExcelPackage = excelPackage;
		    ProviderSummaries = providerSummaries;
		    DatasetDefinition = datasetDefinition;
		    ValidationResult = new DatasetUploadValidationResult();
	    }

	    public ExcelPackage ExcelPackage { get; set; }

        public Func<IEnumerable<ProviderSummary>> ProviderSummaries { get; set; }

        public DatasetDefinition DatasetDefinition { get; set; }

        public TableLoadResultWithHeaders Data { get; set; }

        public IDatasetUploadValidationResult ValidationResult { get; set; }
    }
}
