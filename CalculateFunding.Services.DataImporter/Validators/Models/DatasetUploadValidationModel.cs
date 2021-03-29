using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using OfficeOpenXml;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class DatasetUploadValidationModel
    {
	    public DatasetUploadValidationModel(
            ExcelPackage excelPackage, 
            Func<IEnumerable<ProviderSummary>> providerSummaries, 
            DatasetDefinition datasetDefinition,
            DatasetEmptyFieldEvaluationOption datasetEmptyFieldEvaluationOption,
            string latestBlobFileName)
	    {
		    ExcelPackage = excelPackage;
		    ProviderSummaries = providerSummaries;
		    DatasetDefinition = datasetDefinition;
            DatasetEmptyFieldEvaluationOption = datasetEmptyFieldEvaluationOption;
            LatestBlobFileName = latestBlobFileName;

            ValidationResult = new DatasetUploadValidationResult();
        }

        public ExcelPackage ExcelPackage { get; set; }

        public Func<IEnumerable<ProviderSummary>> ProviderSummaries { get; set; }

        public DatasetDefinition DatasetDefinition { get; set; }

        public TableLoadResultWithHeaders Data { get; set; }

        public IDatasetUploadValidationResult ValidationResult { get; set; }

        public DatasetEmptyFieldEvaluationOption DatasetEmptyFieldEvaluationOption { get; set; }

        public string LatestBlobFileName { get; set; }
    }
}
