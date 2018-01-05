using System.Threading;

namespace CalculateFunding.Models
{
    public class Command
    {
        public string Id { get; set; }
        public Reference User { get; set; }
        public CommandMethod Method { get; set; }
        public string TargetDocumentType { get; set; }
    }
    public abstract class Command<T> : Command, IIdentifiable where T : IIdentifiable
    {
        protected Command()
        {
            TargetDocumentType = typeof(T).Name;
        }
        public T Content { get; set; }
    }
}