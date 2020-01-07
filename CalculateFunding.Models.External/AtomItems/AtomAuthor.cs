using System;

namespace CalculateFunding.Models.External.AtomItems
{
    [Serializable]
    public class AtomAuthor
    {
        public AtomAuthor()
        {
        }

        public AtomAuthor(string email, string name)
        {
            Email = email;
            Name = name;
        }

        public string Email { get; set; }

        public string Name { get; set; }
    }
}