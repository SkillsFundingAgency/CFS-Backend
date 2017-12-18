namespace CalculateFunding.Models
{
    public abstract class Command<T> : IIdentifiable where T : IIdentifiable
    {
        public string Id { get; set; }
        public Reference User { get; set; }
        public string Method { get; set; }
        public string TargetDocumentType { get; set; }
        public T Content { get; set; }
    }
}