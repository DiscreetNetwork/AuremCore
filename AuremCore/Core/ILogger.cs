using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public interface ILogger
    {
        public void Info(string message);
        public void Warn(string message);
        public void Error(string message, Exception? exc = null);
        public void Fatal(string message, Exception? exc = null);
        public void Log(string message, bool suppress = false, string lvl = "INFO");
    }
}
