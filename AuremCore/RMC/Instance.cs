using AuremCore.Crypto.BN256;
using AuremCore.Crypto.Multi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    public class Instance
    {
        private SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);
        private ulong Id;
        private Keychain Keys;
        private int RawLength;
        private byte[] SignedData;
        private Signature Proof;
        private Status Stat;

        public Instance() { }

        public async Task SendData(Stream s)
        {
            await Mutex.WaitAsync();



            Mutex.Release();
        }
    }
}
