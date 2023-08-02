using Aurem.Logging.FastLogger;
using Aurem.Model.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Logging
{
    /// <summary>
    /// Provides utilities to the logger.
    /// </summary>
    public static class LoggingUtil
    {
        public static void AddingErrors(IList<Exception> errors, int size, Logger log)
        {
            if (errors == null || errors.Count == 0)
            {
                log.Info().Val(Constants.Size, size).Msg(Constants.ReadyToAdd);
                return;
            }

            var ok = 0;
            var units = 0;
            var preunits = 0;

            foreach (var err in errors)
            {
                if (err == null)
                {
                    ok++;
                    continue;
                }

                if (err is DuplicateUnitException) { units++; continue; }
                if (err is DuplicatePreunitException) { preunits++; continue; }
                if (err is UnknownParentsException e) { log.Info().Val(Constants.Size, e.Amount).Msg(Constants.UnknownParents); continue; }

                log.Error().Str("where", "AddPreunits").Msg(err.Message);
            }

            if (units > 0)
            {
                log.Info().Val(Constants.Size, units).Msg(Constants.DuplicatedUnits);
            }

            if (preunits > 0)
            {
                log.Info().Val(Constants.Size, preunits).Msg(Constants.DuplicatedPreunits);
            }

            if (ok > 0)
            {
                log.Info().Val(Constants.Size, ok).Msg(Constants.ReadyToAdd);
            }
        }
    }
}
