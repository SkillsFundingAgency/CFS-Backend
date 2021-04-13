using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Users.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamPermissionsUsersCsvTransformServiceLocator
        : IUsersCsvTransformServiceLocator
    {
        private readonly IEnumerable<IUsersCsvTransform> _transforms;

        public FundingStreamPermissionsUsersCsvTransformServiceLocator(IEnumerable<IUsersCsvTransform> transforms)
        {
            Guard.IsNotEmpty(transforms, nameof(transforms));

            _transforms = transforms.ToArray();
        }

        public IUsersCsvTransform GetService(string jobDefinitionName)
        {
            return _transforms.SingleOrDefault(_ => _.IsForJobDefinition(jobDefinitionName)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
