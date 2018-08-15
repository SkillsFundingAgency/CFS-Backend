using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators.Extension
{
    public static class ReasonForFailureColor
    {
	    private static readonly Color Orange = Color.FromArgb(255, 138, 80);
	    private static readonly Color Gold = Color.FromArgb(255, 217, 102);
	    private static readonly Color LightYellow = Color.FromArgb(255, 255, 114);
	    private static readonly Color Blue = Color.FromArgb(122, 124, 255);
	    private static readonly Color LightPink = Color.FromArgb(255, 178, 255);


	    private static Dictionary<FieldValidationResult.ReasonForFailure, Color> ReasonForFailureColorMapping =
		    new Dictionary<FieldValidationResult.ReasonForFailure, Color>
		    {
			    {FieldValidationResult.ReasonForFailure.DataTypeMismatch, Orange},
			    {FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded, Gold},
			    {FieldValidationResult.ReasonForFailure.ProviderIdValueMissing, LightYellow},
			    {FieldValidationResult.ReasonForFailure.DuplicateEntriesInTheProviderIdColumn, Blue},
			    {FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider, LightPink},
		    };

	    

	    public static Color GetColorCodeForFailure(this FieldValidationResult.ReasonForFailure reasonForFailure)
	    {
		    ReasonForFailureColorMapping.TryGetValue(reasonForFailure, out var value);

		    if (value == null)
		    {
			    throw new ArgumentOutOfRangeException($"No color mapping provided for {reasonForFailure}");
		    }
		    return value;
	    }
    }
}
