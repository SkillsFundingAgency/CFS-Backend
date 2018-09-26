using CalculateFunding.Models.Results;
using System.Reflection;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IAllocationFactory
    {
       IAllocationModel CreateAllocationModel(Assembly assembly);
    }
}
