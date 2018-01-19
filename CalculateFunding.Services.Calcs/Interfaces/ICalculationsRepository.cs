using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<HttpStatusCode> CreateDraftCalculation(Calculation calculation);
    }
}
