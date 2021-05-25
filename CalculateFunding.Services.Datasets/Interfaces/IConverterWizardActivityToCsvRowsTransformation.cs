using TemplateMappingItem = CalculateFunding.Common.ApiClient.Calcs.Models.TemplateMappingItem;
using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterWizardActivityToCsvRowsTransformation
    {
        IEnumerable<ExpandoObject> TransformConvertWizardActivityIntoCsvRows(IEnumerable<ProviderConverterDetail> eligibleConverters, IEnumerable<ConverterDataMergeLog> converterDataMergeLogs, IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels);
    }
}