using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    // Service that can be started and stopped
    public interface IService
    {
        public Exception Start();
        public void Stop();
    }
}
