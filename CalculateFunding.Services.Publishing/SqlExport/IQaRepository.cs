namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaRepository
    {
        int ExecuteSql(string sql);
    }
}