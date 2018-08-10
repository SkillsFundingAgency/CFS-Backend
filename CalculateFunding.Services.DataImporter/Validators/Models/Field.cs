using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class Field
    {
	    public Field(DatasetUploadCellReference cellReference, object value, FieldDefinition fieldDefinition)
	    {
		    CellReference = cellReference;
		    Value = value;
		    FieldDefinition = fieldDefinition;
	    }

	    public DatasetUploadCellReference CellReference { get;  }

		public object Value { get;  }

	    public FieldDefinition FieldDefinition { get; }
	}
}
