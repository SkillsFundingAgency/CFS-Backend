namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class HeaderField
    {
	    public HeaderField(string headerName)
	    {
		    HeaderName = headerName;
	    }

	    public string HeaderName { get; }
    }
}
