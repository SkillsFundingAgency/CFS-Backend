using System.Threading.Tasks;
using CalculateFunding.Services.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetCloneBuilderFactory
    {
        IDatasetCloneBuilder CreateCloneBuilder();
    }
}