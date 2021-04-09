using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
    [TestClass]
    public class DuplicateHeaderValidatorTests
    {
        private DuplicateHeaderValidator _validator;

        [TestMethod]
        public void ReportsDuplicateNamesInTheValidatedHeaderFields()
        {
            string fieldOne = NewRandomString();
            string fieldTwo = NewRandomString();
            string fieldThree = NewRandomString();
            string fieldFour = NewRandomString();

            FieldDefinition fieldDefinitionOne = NewFieldDefinition(_ => _.WithName(fieldOne));
            FieldDefinition fieldDefinitionFour = NewFieldDefinition(_ => _.WithName(fieldFour));
			
            GivenTheHeaderValidator(fieldDefinitionOne,
                NewFieldDefinition(_ => _.WithName(fieldTwo)),
                NewFieldDefinition(_ => _.WithName(fieldThree)),
                fieldDefinitionFour);

            IEnumerable<HeaderValidationResult> validationResults = WhenTheHeaderFieldsAreValidated(fieldFour,
                fieldOne,
                fieldTwo,
                fieldFour,
                fieldThree,
                fieldOne);

            validationResults
                .Should()
                .BeEquivalentTo(NewHeaderValidationResult(fieldDefinitionFour),
                    NewHeaderValidationResult(fieldDefinitionOne));
        }

        private HeaderValidationResult NewHeaderValidationResult(FieldDefinition fieldDefinition)
            => new HeaderValidationResultBuilder()
                .WithFieldDefinition(fieldDefinition)
                .WithReasonForFailure(DatasetCellReasonForFailure.DuplicateColumnHeader)
                .WithHasBackgroundKeyColour(true)
                .Build();
		
        private void GivenTheHeaderValidator(params FieldDefinition[] fieldDefinitions)
            => _validator = new DuplicateHeaderValidator(fieldDefinitions);

        private IEnumerable<HeaderValidationResult> WhenTheHeaderFieldsAreValidated(params string[] headerFields)
            => _validator.ValidateHeaders(headerFields.ToList());
		
        private static FieldDefinition NewFieldDefinition(Action<FieldDefinitionBuilder> setUp = null)
        {
            FieldDefinitionBuilder fieldDefinitionBuilder = new FieldDefinitionBuilder();

            setUp?.Invoke(fieldDefinitionBuilder);
			
            return fieldDefinitionBuilder.Build();
        }
		
        private static string NewRandomString() => new RandomString();
    }
}