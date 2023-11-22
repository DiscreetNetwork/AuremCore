using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// ConfigException is returned when a provided configuration cannot be parsed.
    /// </summary>
    public class ConfigException : Exception
    {
        public ConfigException(string msg) : base("ConfigException: " + msg) { }
    }
}
