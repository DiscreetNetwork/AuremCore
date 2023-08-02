using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Logging.FastLogger
{
    public class LoggerContext
    {
        public class LoggerContextField<T>
        {
            private Func<object?, object?> Getter;
            private Action<object?, object?> Setter;
            private MemberInfo Member;
            private LoggerContext Instance;
            
            internal LoggerContextField(Func<object?, object?> getter, Action<object?, object?> setter, LoggerContext instance, MemberInfo info) { Getter = getter; Setter = setter; Instance = instance; Member = info; }

            public LoggerContext Set(T value)
            {
                if (Setter == null) throw new Exception($"Internal Error: property for {((PropertyInfo)Member).DeclaringType?.Name} named \"{((PropertyInfo)Member).Name}\" does not define a setter");
                Setter(Instance, value);
                return Instance;
            }

            public T Get()
            {
                if (Setter == null) throw new Exception($"Internal Error: property for {((PropertyInfo)Member).DeclaringType?.Name} named \"{((PropertyInfo)Member).Name}\" does not define a getter");
                return (T)Getter(Instance);
            }
        }

        public bool TimeStamp { get; private set; } = false;
        public bool StackTrace { get; private set; } = false;
        public LogLvl Level { get; private set; } = LogLvl.NoLevel;
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(1);
        public int Buffer { get; private set; } = 0;
        public string LevelFieldName { get; private set; } = "level";
        public string TimestampFieldName { get; private set; } = "time";
        public string LevelTraceValue { get; private set; } = "level";
        public string LevelDebugValue { get; private set; } = "debug";
        public string LevelInfoValue { get; private set; } = "info";
        public string LevelWarnValue { get; private set; } = "warn";
        public string LevelErrorValue { get; private set; } = "error";
        public string LevelFatalValue { get; private set; } = "fatal";
        public string LevelPanicValue { get; private set; } = "panic";
        public Func<LogLvl, string> LevelFieldMarshalFunc { get; private set; } = x => FastLogger.Logger.LevelFieldMarshalOpen(x);

        public string MessageFieldName { get; private set; } = "message";
        public string ErrorFieldName { get; private set; } = "error";

        public string StackTraceFieldName { get; private set; } = "stack";
        //public Func<StackTrace, object> StackTraceMarshalFunc { get; private set; } = x =>
        //{
        //    return x.GetFrames().Select(f => $"\"{f.ToString()}\"").ToArray();
        //};

        public Func<Exception, string> ErrorMarshalFunc { get; private set; } = x =>
        {
            return /* $"[{x.GetType().Name}] {x.Message}" */ x.Message;
        };

        public Func<DateTime> TimestampFunc { get; private set; } = () => DateTime.Now;

        public string TimeFormat { get; private set; } = "HH:mm:ss:fff";

        public Func<TimeSpan, string> DurationNumberFunc { get; private set; } = t => t.TotalSeconds.ToString() + "s";

        private Logger MyLogger;

        public LoggerContext() { }

        public LoggerContext(Logger logger) { MyLogger = logger; }

        public LoggerContext(LoggerContext other, Logger logger)
        {
            TimeStamp = other.TimeStamp;
            StackTrace = other.StackTrace;
            Level = other.Level;
            Timeout = other.Timeout;
            Buffer = other.Buffer;
            LevelFieldName = other.LevelFieldName;
            TimestampFieldName = other.TimestampFieldName;
            LevelTraceValue = other.LevelTraceValue;
            LevelDebugValue = other.LevelDebugValue;
            LevelInfoValue = other.LevelInfoValue;
            LevelWarnValue = other.LevelWarnValue;
            LevelErrorValue = other.LevelErrorValue;
            LevelFatalValue = other.LevelFatalValue;
            LevelPanicValue = other.LevelPanicValue;
            LevelFieldMarshalFunc = other.LevelFieldMarshalFunc;

            MessageFieldName = other.MessageFieldName;
            ErrorFieldName = other.ErrorFieldName;

            StackTraceFieldName = other.StackTraceFieldName;

            ErrorMarshalFunc = other.ErrorMarshalFunc;

            TimestampFunc = other.TimestampFunc;

            TimeFormat = other.TimeFormat;

            DurationNumberFunc = other.DurationNumberFunc;
            MyLogger = logger;
        }

        public void EnsureLoggerSet(Logger logger) { MyLogger = (MyLogger) ?? logger; }

        public LoggerContext NoTimestamp()
        {
            TimeStamp = false;
            return this;
        }

        public LoggerContext Timestamp()
        {
            TimeStamp = true;
            return this;
        }

        private static MemberInfo GetMember<T, V>(Expression<Func<T, V>> expr)
        {
            if (expr.Parameters.Count < 1) throw new ArgumentException(nameof(expr));
            return GetMember<T>(expr.Body);
        }

        private static MemberInfo GetMember<T>(Expression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));

            if (expr is MemberExpression memberExpr)
            {
                if (!typeof(T).GetProperties().Contains(memberExpr.Member)) throw new ArgumentException("selector must be for target type member");
                //if (!(memberExpr.Member.MemberType == MemberTypes.Property)) throw new ArgumentException("selector must get property");
                return memberExpr.Member;
            }

            if (expr is MethodCallExpression methodCallExpr)
            {
                if (!typeof(T).GetMethods().Contains(methodCallExpr.Method)) throw new ArgumentException("selector must be for target type member");
                return methodCallExpr.Method;
            }

            if (expr is UnaryExpression)
            {
                return GetMember<T>((UnaryExpression)expr);
            }

            throw new ArgumentException("selector is invalid");
        }

        private static MemberInfo GetMember<T>(UnaryExpression expr)
        {
            if (expr.Operand is MethodCallExpression methodCallExpr)
            {
                if (!typeof(T).GetMethods().Contains(methodCallExpr.Method)) throw new ArgumentException("selector must be for target type member");
                return methodCallExpr.Method;
            }
            else
            {
                return GetMember<T>(expr.Operand);
            }
        }

        private Action<object?, object?> GetMemberSetter<T, V>(Expression<Func<T, V>> expr, bool returnNullIfInvalid = false)
        {
            var info = GetMember(expr);
            if (info is PropertyInfo pInfo)
            {
                if (!pInfo.CanRead)
                {
                    if (returnNullIfInvalid) return null;
                    throw new ArgumentException("selected property does not define a setter");
                }
            }
            return info.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)info).SetValue,
                MemberTypes.Property => ((PropertyInfo)info).SetValue,
                _ => throw new ArgumentException("expression must select for a field or property"),
            };
        }

        private Func<object?, object?> GetMemberGetter<T, V>(Expression<Func<T, V>> expr, bool returnNullIfInvalid = false)
        {
            var info = GetMember(expr);
            if (info is PropertyInfo pInfo)
            {
                if (!pInfo.CanRead)
                {
                    if (returnNullIfInvalid) return null;
                    throw new ArgumentException("selected property does not define a getter");
                }
            }
            return info.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)info).GetValue,
                MemberTypes.Property => ((PropertyInfo)info).GetValue,
                _ => throw new ArgumentException("expression must select for a field or property"),
            };
        }

        public LoggerContext WithProperty<T>(Expression<Func<LoggerContext, T>> propertySelector, T value)
        {
            GetMemberSetter(propertySelector)(this, value);
            return this;
        }

        public LoggerContextField<T> WithProperty<T>(Expression<Func<LoggerContext, T>> propertySelector)
        {
            return new LoggerContextField<T>(GetMemberGetter(propertySelector, true), GetMemberSetter(propertySelector, true), this, GetMember(propertySelector));
        }

        public LoggerContext NoStack()
        {
            return WithProperty(x => x.StackTrace, false);
        }

        public LoggerContext Stack()
        {
            return WithProperty(x => x.StackTrace, true);
        }

        public Logger Logger()
        {
            return MyLogger;
        }
    }
}
