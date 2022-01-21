namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaRepositoryLocator
    {
        IQaRepository GetService(SqlExportSource sqlExportSource);
    }
}
