using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Atlas
{
    namespace SnapshotIO
    {
        public class SnapshotVisitorException : Exception
        {
            public SnapshotVisitorException()
            {
            }

            public SnapshotVisitorException(string message)
                : base(message)
            {
            }

            public SnapshotVisitorException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}