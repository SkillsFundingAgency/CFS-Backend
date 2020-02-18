using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsFeatureFlag
    {
        Task<bool> IsGraphEnabled();
    }
}
