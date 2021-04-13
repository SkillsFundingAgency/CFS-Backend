using System.Collections.Generic;
using System.Dynamic;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUsersCsvTransform
    {
        IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents);

        bool IsForJobDefinition(string jobDefinitionName);
    }
}
