using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UndergroundPolice
{
    public static class Utils
    {
        public static void DequeueAll<T>(this ConcurrentQueue<T> self, ICollection<T> other)
        {
            T element;
            while (self.TryDequeue(out element))
            {
                other.Add(element);
            }
        }
    }
}