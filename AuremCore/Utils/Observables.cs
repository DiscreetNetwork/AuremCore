using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Utils
{
    internal struct ObserverPair<T>
    {
        public int Idx;
        public Action<T> Observer;
    }

    internal class ObserverMemo<T> : IObserverManager
    {
        public ObserverPair<T> Id;
        public Observable<T> Manager;

        public void RemoveObserver()
        {
            Manager.RemoveObserver(Id);
        }
    }

    internal class SafeObserverMemo<T> : IObserverManager
    {
        public ObserverPair<T> Id;
        public SafeObservable<T> Manager;
        private readonly object _lock = new object();

        public void RemoveObserver()
        {
            lock (_lock) Manager.RemoveObserver(Id);
        }
    }

    public class Observable<T> : IObservable<T>
    {
        private Dictionary<int, ObserverPair<T>> Observers;
        private int counter;

        public static Observable<T> NewObservable()
        {
            return new Observable<T>();
        }

        public Observable()
        {
            Observers = new();
            counter = 0;
        }

        public IObserverManager AddObserver(Action<T> observer)
        {
            var pair = new ObserverPair<T> { Idx = counter++, Observer = observer };
            Observers[pair.Idx] = pair;
            return new ObserverMemo<T> { Id = pair, Manager = this };
        }

        internal void RemoveObserver(ObserverPair<T> memo)
        {
            Observers.Remove(memo.Idx);
        }

        public void Notify(T data)
        {
            foreach (var obs in Observers.Values)
            {
                obs.Observer(data);
            }
        }
    }

    public class SafeObservable<T> : IObservable<T>
    {
        private ConcurrentDictionary<int, ObserverPair<T>> Observers;
        private int counter;
        private readonly object _lock = new();

        public SafeObservable()
        {
            Observers = new();
            counter = 0;
        }

        public IObserverManager AddObserver(Action<T> observer)
        {
            lock (_lock)
            {
                var pair = new ObserverPair<T> { Idx = Interlocked.Increment(ref counter), Observer = observer };
                Observers.TryAdd(pair.Idx, pair);
                return new SafeObserverMemo<T> { Id = pair, Manager = this };
            }
        }

        internal void RemoveObserver(ObserverPair<T> memo)
        {
            lock (_lock)
            {
                Observers.Remove(memo.Idx, out _);
            }
        }

        public void Notify(T data)
        {
            lock (_lock)
            {
                foreach (var obs in Observers.Values) obs.Observer(data);
            }
        }
    }
}
