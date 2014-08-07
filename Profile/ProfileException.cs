using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas
{
    namespace Profile
    {
        class ProfileException : Exception
        {
            public ProfileException()
            {
            }

            public ProfileException(string message)
                : base(message)
            {
            }

            public ProfileException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}