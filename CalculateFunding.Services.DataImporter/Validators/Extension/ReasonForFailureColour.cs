using System;
using System.Collections.Generic;
using System.Drawing;
using CalculateFunding.Services.DataImporter.Validators.Models;
using Pipelines.Sockets.Unofficial.Arenas;

namespace CalculateFunding.Services.DataImporter.Validators.Extension
{
    public static class ReasonForFailureColour
    {
	    public static readonly Color Orange = Color.FromArgb(255, 138, 80);
	    public static readonly Color Gold = Color.FromArgb(255, 217, 102);
	    public static readonly Color LightYellow = Color.FromArgb(255, 255, 114);
	    public static readonly Color Blue = Color.FromArgb(122, 124, 255);
	    public static readonly Color LightPink = Color.FromArgb(255, 178, 255);
		public static readonly Color LightGreen = Color.FromArgb(146, 208, 80);
		public static readonly Color Red = Color.FromArgb(255, 0, 0);
		public static readonly Color LighterBlue = Color.FromArgb(0, 176, 240);
		public static readonly Color LightBlue = Color.FromArgb(50, 113, 168);

		private static readonly Dictionary<DatasetCellReasonForFailure, Color> ReasonForFailureColorMapping =
		    new Dictionary<DatasetCellReasonForFailure, Color>
		    {
			    {DatasetCellReasonForFailure.DataTypeMismatch, Orange},
			    {DatasetCellReasonForFailure.MaxOrMinValueExceeded, Gold},
			    {DatasetCellReasonForFailure.ProviderIdValueMissing, LightYellow},
			    {DatasetCellReasonForFailure.DuplicateEntriesInTheProviderIdColumn, Blue},
			    {DatasetCellReasonForFailure.ProviderIdMismatchWithServiceProvider, LightPink},
				{DatasetCellReasonForFailure.NewProviderMissingAllDataSchemaFields, LightGreen},
				{DatasetCellReasonForFailure.ExtraHeaderField, Red},
				{DatasetCellReasonForFailure.ProviderIdNotInCorrectFormat, LighterBlue},
				{DatasetCellReasonForFailure.DuplicateColumnHeader, LightBlue}
			};

		public static Color GetColorCodeForFailure(this DatasetCellReasonForFailure reasonForFailure)
			=> ReasonForFailureColorMapping.TryGetValue(reasonForFailure, out Color colour) 
				? colour 
				: throw new ArgumentOutOfRangeException($"No color mapping provided for {reasonForFailure}");
    }
}
