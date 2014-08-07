using System;

namespace Atlas
{
    namespace Core
    {
        public class MetaAssociation
        {
            public string Aggregation { get; set; }

            public string EndClass { get; set; }

            public string EndField { get; set; }

            public string Multiplicity { get; set; }

            public string Description { get; set; }
        }
    }
}