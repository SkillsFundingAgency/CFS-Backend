using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public class ResiliencePolicies : ICalcsResilliencePolicies
    {
        public Policy CalculationsRepository { get; set; }
    }
}
