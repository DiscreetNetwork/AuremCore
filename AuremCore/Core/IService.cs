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
        public Task<Exception?> Start();

        public virtual void Stop() => StopAsync().GetAwaiter().GetResult();

        public Task StopAsync();
    }
}
