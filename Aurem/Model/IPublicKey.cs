using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Used for checking signatures.
    /// </summary>
    public interface IPublicKey
    {
        /// <summary>
        /// Checks if the given preunit has the correct signature with this public key.
        /// </summary>
        /// <param name="preunit"></param>
        /// <returns></returns>
        public bool Verify(IPreunit preunit);

        /// <summary>
        /// Encodes the public key as a base64-encoded string.
        /// </summary>
        /// <returns></returns>
        public string Encode();
    }
}
