using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Produces an IRandomSource for the given DAG.
    /// </summary>
    public interface IRandomSourceFactory
    {
        /// <summary>
        /// Produces a randomness source for the provided DAG.
        /// </summary>
        /// <param name="dag">The DAG to provide a randomness source for.</param>
        /// <returns>An <see cref="IRandomSource"/> to use for the DAG.</returns>
        public IRandomSource CreateRandomSource(IDag dag);

        /// <summary>
        /// Returns random source data which should be included in the dealing unit for the given epoch.
        /// </summary>
        /// <param name="epochId">The epoch corresponding to the dealing unit.</param>
        /// <returns>Random source data corresponding to the given epoch's dealing unit.</returns>
        public (byte[], Exception?) DealingData(uint epochId);
    }
}
