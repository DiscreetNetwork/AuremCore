using Aurem.Config;
using Aurem.Logging.FastLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aurem.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public static class ALogger
    {
        public static LoggerContext BaseContext { get; } = new();

        public static DateTime Genesis { get; }

        static ALogger()
        {
            Genesis = DateTime.Now;
            BaseContext.WithProperty(x => x.TimestampFieldName, Constants.Time);
            BaseContext.WithProperty(x => x.LevelFieldName, Constants.Level);
            BaseContext.WithProperty(x => x.MessageFieldName, Constants.Message);

            BaseContext.WithProperty(x => x.TimestampFunc, () => new DateTime(DateTime.Now.Subtract(Genesis).Ticks));
            BaseContext.WithProperty(x => x.LevelFieldMarshalFunc, x => ((int)x).ToString());
        }

        public static Logger NewLogger(Config.Config conf)
        {
            string filename;

            if (conf.LogHuman)
            {
                filename = conf.LogFile + ".log";
            } else
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
            log.Log().Str(Constants.Genesis, Genesis.ToLongTimeString());

            return log;
        }
    }
}
