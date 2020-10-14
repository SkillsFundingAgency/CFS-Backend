using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexDataReader<TK, TT> where TT : class
    {
        Task<TT> GetData(TK key);
    }
}
