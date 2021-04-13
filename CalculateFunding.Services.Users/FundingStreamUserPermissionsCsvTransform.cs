using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Users.Interfaces;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CalculateFunding.Services.Users
{
    public class FundingStreamUserPermissionsCsvTransform : IUsersCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool
            = ArrayPool<ExpandoObject>.Create(FundingStreamPermissionsUsersCsvGenerator.BatchSize, 4);

        public bool IsForJobDefinition(string jobDefinitionName)
        {
            return jobDefinitionName == JobConstants.DefinitionNames.GenerateFundingStreamPermissionsCsvJob;
        }

        public IEnumerable<ExpandoObject> Transform(IEnumerable<dynamic> documents)
        {
            int resultsCount = documents.Count();
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                UserFundingStreamPermission userFundingStreamPermission
                    = documents.ElementAt(resultCount);

                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                row["User"] = userFundingStreamPermission.User.Username;

                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanAdministerFundingStream)] 
                    = userFundingStreamPermission.FundingStreamPermission.CanAdministerFundingStream;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanCreateSpecification)]
                    = userFundingStreamPermission.FundingStreamPermission.CanCreateSpecification;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanEditSpecification)]
                    = userFundingStreamPermission.FundingStreamPermission.CanEditSpecification;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveSpecification)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveSpecification;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanDeleteSpecification)]
                    = userFundingStreamPermission.FundingStreamPermission.CanDeleteSpecification;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanEditCalculations)]
                    = userFundingStreamPermission.FundingStreamPermission.CanEditCalculations;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanDeleteCalculations)]
                    = userFundingStreamPermission.FundingStreamPermission.CanDeleteCalculations;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanMapDatasets)]
                    = userFundingStreamPermission.FundingStreamPermission.CanMapDatasets;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanChooseFunding)]
                    = userFundingStreamPermission.FundingStreamPermission.CanChooseFunding;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanRefreshFunding)]
                    = userFundingStreamPermission.FundingStreamPermission.CanRefreshFunding;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveFunding)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveFunding;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanReleaseFunding)]
                    = userFundingStreamPermission.FundingStreamPermission.CanReleaseFunding;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanCreateProfilePattern)]
                    = userFundingStreamPermission.FundingStreamPermission.CanCreateProfilePattern;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanEditProfilePattern)]
                    = userFundingStreamPermission.FundingStreamPermission.CanEditProfilePattern;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanDeleteProfilePattern)]
                    = userFundingStreamPermission.FundingStreamPermission.CanDeleteProfilePattern;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanAssignProfilePattern)]
                    = userFundingStreamPermission.FundingStreamPermission.CanAssignProfilePattern;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApplyCustomProfilePattern)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApplyCustomProfilePattern;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveCalculations)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveCalculations;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveAnyCalculations)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveAnyCalculations;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanUploadDataSourceFiles)]
                    = userFundingStreamPermission.FundingStreamPermission.CanUploadDataSourceFiles;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanCreateTemplates)]
                    = userFundingStreamPermission.FundingStreamPermission.CanCreateTemplates;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanEditTemplates)]
                    = userFundingStreamPermission.FundingStreamPermission.CanEditTemplates;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanDeleteTemplates)]
                    = userFundingStreamPermission.FundingStreamPermission.CanDeleteTemplates;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveTemplates)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveTemplates;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanRefreshPublishedQa)]
                    = userFundingStreamPermission.FundingStreamPermission.CanRefreshPublishedQa;
                row[nameof(userFundingStreamPermission.FundingStreamPermission.CanApproveAllCalculations)]
                    = userFundingStreamPermission.FundingStreamPermission.CanApproveAllCalculations;

                yield return (ExpandoObject)row;
            }

            _expandoObjectsPool.Return(resultsBatch);
        }
    }
}
