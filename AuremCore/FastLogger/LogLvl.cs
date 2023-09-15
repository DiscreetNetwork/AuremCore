using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.FastLogger
{
    public enum LogLvl
    {
        Disabled = 7,
        NoLevel = 6,
        PanicLevel = 5,
        FatalLevel = 4,
        ErrorLevel = 3,
        WarnLevel = 2,
        InfoLevel = 1,
        DebugLevel = 0,
        TraceLevel = -1,
        All = -2,
    }


}
