namespace CalculateFunding.Services.SqlExport.Models
{
    public class SqlColumnDefinition
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool AllowNulls { get; set; }
    }
}
