using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public interface IWorkerPool
    {
        public void Start();
        public void Stop();

        public Task StopAsync();
    }
}
