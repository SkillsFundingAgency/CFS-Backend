using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexTrasformer<TT, TW> where TT : class where TW : class
    {
        Task<TW> Transform(TT entity, ISearchIndexProcessorContext context);
    }
}
