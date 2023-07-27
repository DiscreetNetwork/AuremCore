using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Utils
{
    /// <summary>
    /// Observable allows one to attach a callback which will be called when someone calls the Notify method. It might be, for example, a change of some property's value.
    /// </summary>
    public interface IObservable<T>
    {
        /// <summary>
        /// Adds a new callback to an observable value.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns>An <see cref="IObserverManager"/> corresponding to the attached observer.</returns>
        public IObserverManager AddObserver(Action<T> observer);

        /// <summary>
        /// Executes all callbacks with the provided data.
        /// </summary>
        /// <param name="data"></param>
        public void Notify(T data);
    }
}
