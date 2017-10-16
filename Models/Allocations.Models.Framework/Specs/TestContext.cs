using System.Collections.Generic;

namespace Allocations.Models.Framework.Specs
{
    public class TestContext
    {
        public TestContext()
        {
            Datasets = new Dictionary<string, object>();
        }

        public string ModelName { get; set; }
        public Dictionary<string, object> Datasets { get; private set; }
    }
}
