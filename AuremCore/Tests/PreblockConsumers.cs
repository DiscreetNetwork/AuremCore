using AuremCore.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public static class PreblockConsumers
    {
        public static async Task<string> NopPreblockConsumer(ChannelReader<Preblock> ps)
        {
            await foreach (var _ in ps.ReadAllAsync()) ;
            return "";
        }

        public static async Task<string> CountingPreblockConsumer(ChannelReader<Preblock> ps)
        {
            var n = 0;
            await foreach (var _ in ps.ReadAllAsync()) n++;

            return $"Preblocks consumed: {n}\n";
        }

        public static async Task<string> PrintingPreblockConsumer(int pid, ChannelReader<Preblock> ps)
        {
            await foreach (var pp in ps.ReadAllAsync())
            {
                await Console.Out.WriteLineAsync($"PrintingPreblockConsumer: pid={pid}: preblock received");
            }

            return "null";
        }

        public static async Task<string> ControlSumPreblockConsumer(ChannelReader<Preblock> ps)
        {
            var n = 0;
            var last = new byte[32];
            var hs = new MemoryStream(4096);

            await foreach (var pb in ps.ReadAllAsync())
            {
                n++;
                hs.Write(last);
                foreach (var data in pb.Data)
                {
                    if (data != null) hs.Write(data);
                }
                if (pb.RandomBytes != null) hs.Write(pb.RandomBytes);
                last = SHA256.HashData(hs.ToArray());
                hs.SetLength(0);
            }

            return $"Preblocks consumed: {n}\nControl sum: {Convert.ToBase64String(last)}\n";
        }

        public static async Task<string> DataExtractingPreblockConsumer(ChannelReader<Preblock> ps)
        {
            throw new NotImplementedException();
        }
    }
}
