using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Used for signing units.
    /// </summary>
    public interface IPrivateKey
    {
        /// <summary>
        /// Signs the preunit and returns the encoded signature.
        /// </summary>
        /// <param name="hash">The hash of the preunit.</param>
        /// <returns></returns>
        public byte[] Sign(Hash hash);

        /// <summary>
        /// Encodes the private key as a base64-encoded string.
        /// </summary>
        /// <returns></returns>
        public string Encode();
    }
}
