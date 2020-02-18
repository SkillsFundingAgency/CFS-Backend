using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface ISpecificationRepository
    {
        Task DeleteSpecification(string specificationId);

        Task UpsertSpecifications(IEnumerable<Specification> specifications);
        
        Task DeleteAllForSpecification(string specificationId);
    }
}
