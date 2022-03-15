using CalculateFunding.Services.Core.Interfaces;
using System;

namespace CalculateFunding.Tests.Common
{
    public class SequentialGuidIdentifierGenerator : IUniqueIdentifierProvider
    {
        public int NextId { get; set; }

        public SequentialGuidIdentifierGenerator()
        {
            NextId = 1;
        }

        public SequentialGuidIdentifierGenerator(int startNumber)
        {
            NextId = startNumber;
        }

        public string CreateUniqueIdentifier()
        {
            return GenerateIdentifier().ToString();
        }

        public Guid GenerateIdentifier()
        {
            byte[] context = BitConverter.GetBytes(NextId);
            byte[] contents = new byte[16];

            contents[12] = context[3];
            contents[13] = context[2];
            contents[14] = context[1];
            contents[15] = context[0];


            NextId++;

            return new Guid(contents);
        }
    }
}
