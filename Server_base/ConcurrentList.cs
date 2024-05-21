using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_base
{
    internal class ConcurrentList<T> : List<T>
    {
        private readonly object locker = new();
        private readonly List<T> list = [];
        public ConcurrentList() { }
        public new void Add(T item)
        {
            lock(locker) { list.Add(item); }
        }
        public new void Remove(T item) { 
            lock(locker) { list.Remove(item); } 
        }
    }
}
