namespace CalculateFunding.Models.External
{
    public class AtomAuthor
    {
        public AtomAuthor(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; set; }

        public string Name { get; set; }
    }
}