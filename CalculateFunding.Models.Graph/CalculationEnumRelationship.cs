using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class CalculationEnumRelationship
    {
        public const string ToIdField = "IsReferencedInCalculation";
        public const string FromIdField = "ReferencesEnum";

        public Calculation Calculation { get; set; }

        public Enum Enum { get; set; }
    }
}