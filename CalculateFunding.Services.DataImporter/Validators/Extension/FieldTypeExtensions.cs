using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.DataImporter.Validators.Extension
{
	public static class FieldTypeExtensions
	{
		public static int CompareTo(FieldType fieldType, object value, object constraint)
		{
			if (fieldType == FieldType.Char)
			{
				char v = Convert.ToChar(constraint);
				char c = Convert.ToChar(constraint);
				return CompareHelper<char>(v, c);
			}
			if (fieldType == FieldType.Byte)
			{
				byte v = Convert.ToByte(value);
				byte c = Convert.ToByte(constraint);
				return CompareHelper<byte>(v, c);
			}
			if (fieldType == FieldType.Integer)
			{
				Int64 v = Convert.ToInt64(value);
				Int64 c = Convert.ToInt64(constraint);
				return CompareHelper<byte>(v, c);
			}
			if (fieldType == FieldType.Float)
			{
				float v = Convert.ToSingle(value);
				float c = Convert.ToSingle(constraint);
				return CompareHelper<byte>(v, c);
			}
			if (fieldType == FieldType.Float)
			{
				float v = Convert.ToSingle(value);
				float c = Convert.ToSingle(constraint);
				return CompareHelper<byte>(v, c);
			}
			if (fieldType == FieldType.Decimal)
			{
				decimal v = Convert.ToDecimal(value);
				decimal c = Convert.ToDecimal(constraint);
				return CompareHelper<byte>(v, c);
			}
			if (fieldType == FieldType.DateTime)
			{
				DateTime v = Convert.ToDateTime(value);
				DateTime c = Convert.ToDateTime(constraint);
				return CompareHelper<byte>(v, c);
			}

			throw new ArgumentOutOfRangeException(nameof(fieldType), $"Unexpected type of {fieldType}");
		}

		private static int CompareHelper<T>(object value, object constraint) where T : IComparable
		{
			var valueC = value as IComparable;
			var constraintC = constraint as IComparable;

			return valueC.CompareTo(constraintC);
		}
	}
}
