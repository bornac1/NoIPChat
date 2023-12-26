using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class VersionException : Exception
    {
        public VersionException() { }
        public VersionException(string message) : base(message)
        {
        }
        public VersionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
