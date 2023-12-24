using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transport
{
    public class TransportException : Exception
    {
        public TransportException() { }
        public TransportException(string message) : base(message)
        {
        }
        public TransportException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
