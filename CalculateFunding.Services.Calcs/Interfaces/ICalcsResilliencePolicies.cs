using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalcsResilliencePolicies
    {
        Policy CalculationsRepository { get; set; }
    }
}
