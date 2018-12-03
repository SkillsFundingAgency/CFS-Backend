using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Jobs
{
    public class CreateJobValidationModel
    {
        public JobCreateModel JobCreateModel { get; set; }

        public JobDefinition JobDefinition { get; set; }
    }
}
