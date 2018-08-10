using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class PublishedAllocationResultsModel
    {
        public string SpecificationId { get; set; }

        public UpdatePublishedAllocationLineResultStatusModel UpdateModel { get; set; }

        public IEnumerable<PublishedProviderResult> PublishedProviderResults { get; set; }
    }
}
