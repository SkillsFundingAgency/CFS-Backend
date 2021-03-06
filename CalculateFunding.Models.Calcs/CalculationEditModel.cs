﻿using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationEditModel
    {
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public string Name { get; set; }

        public CalculationValueType? ValueType { get; set; }

        public CalculationDataType DataType { get; set; }

        public string SourceCode { get; set; }

        public IEnumerable<string> AllowedEnumTypeValues { get; set; }

        public string Description { get; set; }
    }
}
