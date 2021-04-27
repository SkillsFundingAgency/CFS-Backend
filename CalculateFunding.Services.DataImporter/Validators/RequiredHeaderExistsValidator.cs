﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
	public class RequiredHeaderExistsValidator : IHeaderValidator
    {
	    private readonly FieldDefinition[] _fieldDefinitions;

		public RequiredHeaderExistsValidator(IEnumerable<FieldDefinition> fieldDefinitions)
	    {
		    _fieldDefinitions = fieldDefinitions.ToArray();
	    }
		
	    public IEnumerable<HeaderValidationResult> ValidateHeaders(IEnumerable<string> headerFields)
	    {
		    HashSet<string> headerFieldNames = headerFields
			    .Select(_ => _.ToLowerInvariant())
			    .ToHashSet();
		    
		    return _fieldDefinitions
			    .Where(f => !headerFieldNames.Contains(f.Name.ToLowerInvariant()))
			    .Select(f => new HeaderValidationResult(f))
			    .ToArray();
	    }
    }
}