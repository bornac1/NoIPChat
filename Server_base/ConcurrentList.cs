namespace Server_base
{
    public class ConcurrentList<T> : List<T>
    {
        private readonly object locker = new();
        private readonly List<T> list = [];
        public ConcurrentList() { }
        public new void Add(T item)
        {
            lock (locker) { list.Add(item); }
        }
        public new void Remove(T item)
        {
            lock (locker) { list.Remove(item); }
        }
    }
}
