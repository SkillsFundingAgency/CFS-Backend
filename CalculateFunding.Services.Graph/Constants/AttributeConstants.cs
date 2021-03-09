using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using Enum = CalculateFunding.Models.Graph.Enum;

namespace CalculateFunding.Services.Graph.Constants
{
    public class AttributeConstants
    {
        public const string DatasetId = Dataset.IdField;
        public const string DatasetDefinitionId = DatasetDefinition.IdField;
        public const string DataFieldId = DataField.IdField;
        public const string CalculationId = Calculation.IdField;
        public const string SpecificationId = Specification.IdField;
        public const string FundingLineId = FundingLine.IdField;
        public const string EnumId = Enum.IdField;
        public const string CalculationEnumRelationshipId = CalculationEnumRelationship.ToIdField;
        public const string EnumCalculationRelationshipId = CalculationEnumRelationship.FromIdField;
        public const string SpecificationCalculationRelationshipId = SpecificationCalculationRelationships.ToIdField;
        public const string CalculationSpecificationRelationshipId = SpecificationCalculationRelationships.FromIdField;
        public const string CalculationACalculationBRelationship = CalculationRelationship.ToIdField;
        public const string CalculationBCalculationARelationship = CalculationRelationship.FromIdField;
        public const string CalculationDataFieldRelationshipId = CalculationDataFieldRelationship.FromIdField;
        public const string DataFieldCalculationRelationship = CalculationDataFieldRelationship.ToIdField;
        public const string DatasetDatasetDefinitionRelationshipId = DatasetDatasetDefinitionRelationship.FromIdField;
        public const string DatasetDefinitionDatasetRelationshipId = DatasetDatasetDefinitionRelationship.ToIdField;
        public const string DataFieldDatasetRelationshipId = DatasetDataFieldRelationship.FromIdField;
        public const string DatasetDataFieldRelationshipId = DatasetDataFieldRelationship.ToIdField; 
        public const string FundingLineCalculationRelationshipId = FundingLineCalculationRelationship.FromIdField;
        public const string CalculationFundingLineRelationshipId = FundingLineCalculationRelationship.ToIdField; 
        public const string SpecificationDatasetRelationship = "ReferencesDataset";
        public const string DatasetSpecificationRelationship = "IsReferencedBySpecification";
    }
}
