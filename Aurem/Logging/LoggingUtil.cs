using AuremCore.FastLogger;
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
        /// <summary>
        /// Logs information about errors from AddPreunits to the provided logger. The size argument is needed to know how many preunits were added in the case everything went okay and errors is null.
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="size"></param>
        /// <param name="log"></param>
        public static void AddingErrors(IList<Exception>? errors, int size, Logger log)
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

        /// <summary>
        /// The base context used for consensus loggers.
        /// </summary>
        public static LoggerContext BaseContext { get; } = new();

        /// <summary>
        /// The timestamp of when the program began.
        /// </summary>
        public static DateTime Genesis { get; }

        static LoggingUtil()
        {
            Genesis = DateTime.Now;
            BaseContext.WithProperty(x => x.TimestampFieldName, Constants.Time);
            BaseContext.WithProperty(x => x.LevelFieldName, Constants.Level);
            BaseContext.WithProperty(x => x.MessageFieldName, Constants.Message);

            BaseContext.WithProperty(x => x.TimestampFunc, () => new DateTime(DateTime.Now.Subtract(Genesis).Ticks));
            BaseContext.WithProperty(x => x.LevelFieldMarshalFunc, x => ((int)x).ToString());
        }

        /// <summary>
        /// Creates a new FastLogger logger based on the given config values.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static Logger NewLogger(Config.Config conf)
        {
            string filename;

            if (conf.LogHuman)
            {
                filename = conf.LogFile + ".log";
            }
            else
            {
                filename = conf.LogFile + ".json";
            }

            Stream output;
            try
            {
                output = File.Create(filename);
            }
            catch
            {
                return Logger.New(Console.OpenStandardError()).Level(LogLvl.Disabled);
            }

            if (conf.LogHuman)
            {
                output = new Decoder(output);
            }

            if (conf.LogBuffer > 0)
            {
                // do nothing for now, as our FastLogger does not support buffering
            }

            var log = Logger.New(output, BaseContext).With().Timestamp().Logger().Level((LogLvl)conf.LogLevel);
            log.Log().Str(Constants.Genesis, Genesis.ToLongTimeString()).Msg(Constants.Genesis);

            return log;
        }
    }
}
