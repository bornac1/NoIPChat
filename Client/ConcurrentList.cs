namespace Client
{
    /// <summary>
    /// Concurrent list.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    public class ConcurrentList<T> : List<T>
    {
        private readonly object locker = new();
        private readonly List<T> list = [];
        /// <summary>
        /// Concurrent list.
        /// </summary>
        public ConcurrentList() { }
        /// <summary>
        /// Adds item in therad-safe way.
        /// </summary>
        /// <param name="item">Item.</param>
        public new void Add(T item)
        {
            lock (locker) { list.Add(item); }
        }
        /// <summary>
        /// Removes item in thread-safe way.
        /// </summary>
        /// <param name="item">Item.</param>
        public new void Remove(T item)
        {
            lock (locker) { list.Remove(item); }
        }
    }
}
