using System;
using System.Reflection;
using System.Text;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IAllocationFactory
    {
       IAllocationModel CreateAllocationModel(Assembly assembly);
    }
}
