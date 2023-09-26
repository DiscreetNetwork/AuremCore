using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.FastLogger
{
    /// <summary>
    /// Logger provides a means of logging data. Allows JSON log output as well as stdout printing.
    /// </summary>
    public class Logger
    {
        internal LoggerContext Context { get; private set; }

        private ConcurrentQueue<LogEvent> EventQueue { get; set; } = new();

        private Stream Base { get; set; }

        public static Logger New(Stream s, LoggerContext ctx = null)
        {
            Logger logger = new()
            {
                Base = s,
            };

            ctx ??= new LoggerContext(logger);
            logger.Context = ctx;
            ctx.EnsureLoggerSet(logger);

            return logger;
        }

        public void Start()
        {
            _ = Task.Run(async () => await Start(default));
        }

        public async Task Start(CancellationToken token)
        {
            var writer = new StreamWriter(Base, Encoding.UTF8);
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5, token);

                while (!EventQueue.IsEmpty)
                {
                    var success = EventQueue.TryDequeue(out var e);
                    if (!success)
                    {
                        throw new Exception("error encountered when dequeueing from the event queue");
                    }
                    await writer.WriteLineAsync(e!.EncodeData().AsMemory(), token);
                }

                await writer.FlushAsync();
            }
        }

        public LoggerContext With()
        {
            return new LoggerContext(Context, this);
        }

        internal void DispatchEvent(LogEvent e) => EventQueue.Enqueue(e);

        public void Update(Func<LoggerContext, LoggerContext> func) => func(Context);

        public Logger Level(LogLvl lvl)
        {
            Context.WithProperty(x => x.Level).Set(lvl);
            return this;
        }

        public LogEvent Trace() => NewEvent(LogLvl.TraceLevel, null);

        public LogEvent Debug() => NewEvent(LogLvl.DebugLevel, null);

        public LogEvent Info() => NewEvent(LogLvl.InfoLevel, null);

        public LogEvent Warn() => NewEvent(LogLvl.WarnLevel, null);

        public LogEvent Error() => NewEvent(LogLvl.ErrorLevel, null);

        public LogEvent Err(Exception exc)
        {
            if (exc == null) return Error().Err(exc);

            return Info();
        }

        public LogEvent Fatal() => NewEvent(LogLvl.FatalLevel, x => Environment.Exit(1));

        public LogEvent Panic() => NewEvent(LogLvl.PanicLevel, x => throw new Exception(x));

        public LogEvent Log() => NewEvent(LogLvl.NoLevel, null);

        public LogLvl GetLevel() => Context.Level;

        public string LevelFieldMarshal(LogLvl level) => level switch
        {
            LogLvl.InfoLevel => Context.LevelInfoValue,
            LogLvl.WarnLevel => Context.LevelWarnValue,
            LogLvl.ErrorLevel => Context.LevelErrorValue,
            LogLvl.FatalLevel => Context.LevelFatalValue,
            LogLvl.PanicLevel => Context.LevelPanicValue,
            LogLvl.DebugLevel => Context.LevelDebugValue,
            LogLvl.TraceLevel => Context.LevelTraceValue,
            _ => $"\"{Level}\""
        };

        private static readonly Logger dummyInstanceForLevelFieldMarshal = Logger.New(null);
        public static string LevelFieldMarshalOpen(LogLvl level) => dummyInstanceForLevelFieldMarshal.LevelFieldMarshal(level);

        public LogEvent NewEvent(LogLvl lvl, Action<string>? done)
        {
            var enabled = Context.Level > lvl;
            if (!enabled)
            {
                if (done != null)
                {
                    done("");
                }

                return null;
            }

            var e = LogEvent.Create(lvl, this);
            e.Done = done;
            if (lvl != LogLvl.NoLevel && Context.LevelFieldName != "")
            {
                e.Str(Context.LevelFieldName, LevelFieldMarshal(lvl));
            }

            foreach ((var k, var v) in Context.DefaultItems) e.Val(k, v);

            if (Context.StackTrace)
            {
                e.Stack = true;
            }

            return e;
        }
    }
}
