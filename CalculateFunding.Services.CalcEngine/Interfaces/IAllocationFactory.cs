using System.Reflection;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAllocationFactory
    {
       IAllocationModel CreateAllocationModel(Assembly assembly);
    }
}
