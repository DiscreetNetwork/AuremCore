using AuremCore.Core;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuremCore.FastLogger
{
    public class LogEvent
    {
        public LogLvl Level { get; set; }
        public bool Stack { get; set; }

        public Dictionary<string, object> Values = new();

        //public StreamWriter Writer { get; set; }
        public Action<string> Done { get; set; }
        public Logger Logger { get; set; }

        public void Write()
        {
            if (Level != LogLvl.Disabled)
            {
                Logger.DispatchEvent(this);
            }
        }

        public static LogEvent Create(LogLvl lvl, Logger logger)
        {
            return new LogEvent
            {
                Level = lvl,
                Stack = false,
                Done = x => { },
                //Writer = new StreamWriter(s),
                Logger = logger,
            };
        }

        internal string EncodeData()
        {
            if (Logger.Context.TimeStamp)
            {
                if (!Values.ContainsKey(Logger.Context.TimestampFieldName)) Timestamp();
            }

            return JsonSerializer.Serialize(Values);
        }

        internal (string Message, string Timestamp, string Level, List<(string K, string V)> Fields) EncodeDataForPrinting()
        {
            if (Logger.Context.TimeStamp)
            {
                if (!Values.ContainsKey(Logger.Context.TimestampFieldName)) Timestamp();
            }

            var rv = new List<(string, string)>();
            var msg = (string)Values[Logger.Context.MessageFieldName];
            var timestamp = (string)Values[Logger.Context.TimestampFieldName];
            var lvl = (string)Values[Logger.Context.LevelFieldName];
            
            if (lvl == Logger.Context.LevelDebugValue)
            {
                lvl = "DBG";
            }
            else if (lvl == Logger.Context.LevelTraceValue)
            {
                lvl = "LVL";
            }
            else if (lvl == Logger.Context.LevelInfoValue)
            {
                lvl = "INF";
            }
            else if (lvl == Logger.Context.LevelWarnValue)
            {
                lvl = "WRN";
            }
            else if (lvl == Logger.Context.LevelErrorValue)
            {
                lvl = "ERR";
            }
            else if (lvl == Logger.Context.LevelPanicValue)
            {
                lvl = "PNC";
            }
            else if (lvl == Logger.Context.LevelFatalValue)
            {
                lvl = "FTL";
            }
            else
            {
                lvl = "LVL";
            }

            foreach ((var k, var v) in Values)
            {
                if (k == Logger.Context.MessageFieldName || k == Logger.Context.TimestampFieldName) continue;

                rv.Add((k, JsonSerializer.Serialize(v)));
            }

            return (msg, timestamp, lvl, rv);
        }

        public LogEvent Str(string k, string v)
        {
            Values[k] = v;
            return this;
        }

        public LogEvent Vals<T>(string k, IEnumerable<T> v)
        {
            Values[k] = v;
            return this;
        }

        public LogEvent Stringer(string k, object v)
        {
            Values[k] = v?.ToString();
            return this;
        }

        public LogEvent Strs(string k, IEnumerable<string> v)
        {
            Values[k] = v;
            return this;
        }

        public LogEvent Hex(string k, ReadOnlySpan<byte> v)
        {
            Values[k] = Convert.ToHexString(v);
            return this;
        }

        public LogEvent Base64(string k, ReadOnlySpan<byte> v)
        {
            Values[k] = Convert.ToBase64String(v);
            return this;
        }

        public LogEvent AnErr(string k, Exception exc)
        {
            Values[k] = Logger.Context.ErrorMarshalFunc(exc);
            return this;
        }

        public LogEvent Errs(string k, IEnumerable<Exception> exc)
        {
            Values[k] = exc.Select(x => Logger.Context.ErrorMarshalFunc(x)).ToArray();
            return this;
        }

        public LogEvent Err(Exception exc)
        {
            if (Stack)
            {
                Values[Logger.Context.StackTraceFieldName] = Regex.Escape(exc.StackTrace ?? "")[1..^1];
            }

            AnErr(Logger.Context.ErrorFieldName, exc);
            return this;
        }

        public LogEvent Val<T>(string k, T v)
        {
            Values[k] = v;
            return this;
        }

        public LogEvent Timestamp()
        {
            Values[Logger.Context.TimestampFieldName] = Logger.Context.TimestampFunc().ToString(Logger.Context.TimeFormat);
            return this;
        }

        public LogEvent Time(string k, DateTime v)
        {
            Values[k] = v.ToString(Logger.Context.TimeFormat);
            return this;
        }

        public LogEvent Time(string k, IEnumerable<DateTime> v)
        {
            Values[k] = v.Select(x => x.ToString(Logger.Context.TimeFormat)).ToArray();
            return this;
        }

        public LogEvent Dur(string k, TimeSpan v)
        {
            Values[k] = Logger.Context.DurationNumberFunc(v);
            return this;
        }

        public LogEvent Durs(string k, IEnumerable<TimeSpan> v)
        {
            Values[k] = v.Select(t => Logger.Context.DurationNumberFunc(t)).ToArray();
            return this;
        }

        public void Msg(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                Values[Logger.Context.MessageFieldName] = msg;
            }

            Write();
            Done(msg);
        }

        public void Msg(string format, params object[] args)
        {
            Msg(string.Format(format, args));
        }

        public void Msg(Func<string> createMsg)
        {
            Msg(createMsg());
        }

        public void Send() => Msg(string.Empty);

        public LogEvent Discard()
        {
            Level = LogLvl.Disabled;
            return null;
        }
    }
}
