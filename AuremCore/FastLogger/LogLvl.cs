using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.FastLogger
{
    public enum LogLvl
    {
        PanicLevel = 5,
        FatalLevel = 4,
        ErrorLevel = 3,
        WarnLevel = 2,
        InfoLevel = 1,
        DebugLevel = 0,
        TraceLevel = -1,
        Disabled = -3,
        NoLevel = -2,
        All = -5,
    }


}
