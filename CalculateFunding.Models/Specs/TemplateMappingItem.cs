namespace CalculateFunding.Models.Specs
{
    public class TemplateMappingItem
    {
        public TemplateMappingEntityType EntityType { get; set; }
        public string Name { get; set; }
        public uint TemplateId { get; set; }
        public string CalculationId { get; set; }
    }
}
