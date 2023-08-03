using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuremCore.Core;

namespace Aurem.Model
{
    /// <summary>
    /// Adder is a component which accepts incoming preunits.
    /// </summary>
    public interface IAdder
    {
        /// <summary>
        /// Adds preunits received from the given process.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="preunits"></param>
        /// <returns></returns>
        public Task<List<Exception>?> AddPreunits(ushort id, params IPreunit[] preunits);

        /// <summary>
        /// Closes the adder.
        /// </summary>
        public Task Close();
    }
}
