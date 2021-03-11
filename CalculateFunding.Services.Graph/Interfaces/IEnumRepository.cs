using CalculateFunding.Models.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IEnumRepository
    {
        Task DeleteEnum(string enumNameValue);

        Task UpsertEnums(IEnumerable<Enum> enums);

        Task UpsertCalculationEnumRelationship(string enumNameValue, string calculationId);

        Task DeleteCalculationEnumRelationship(string enumNameValue, string calculationId);

        Task<IEnumerable<Entity<Enum, IRelationship>>> GetAllEntities(string enumNameValue);
        Task DeleteEnums(IEnumerable<string> enumNameValues);
        Task UpsertCalculationEnumRelationships(params (string calculationId, string enumId)[] relationships);
        Task DeleteCalculationEnumRelationships(params (string calculationId, string enumNameValue)[] relationships);
        Task<IEnumerable<Entity<Enum, IRelationship>>> GetAllEntitiesForAll(params string[] enumNameValues);
    }
}
