using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Utils
{
    /// <summary>
    /// Allows one to remove its callback from its underlying IObservable instance.
    /// </summary>
    public interface IObserverManager
    {
        /// <summary>
        /// Removes the underlying callback from its Observable value.
        /// </summary>
        public void RemoveObserver();
    }
}
