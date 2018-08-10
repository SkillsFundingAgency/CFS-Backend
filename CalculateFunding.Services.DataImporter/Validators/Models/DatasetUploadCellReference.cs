namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class DatasetUploadCellReference
    {
        public DatasetUploadCellReference(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }

        public int RowIndex { get; }

        public int ColumnIndex { get; }
		}
}
