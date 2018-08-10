using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators.Extension
{
    public static class ReasonForFailureColor
    {
	    private static Dictionary<FieldValidationResult.ReasonForFailure, Color> ReasonForFailureColorMapping =
		    new Dictionary<FieldValidationResult.ReasonForFailure, Color>
		    {
			    {FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider, Color.FromArgb(255,178,255)},
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
