using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Publishing
{
    public class TemplateMappingItem
    {
        public TemplateMappingEntityType EntityType { get; set; }
        public string Name { get; set; }
        public uint TemplateId { get; set; }
        public string CalculationId { get; set; }
    }
}
