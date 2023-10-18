using Aurem.Logging;
using Aurem.Model;
using Aurem.Serialize;
using AuremCore.Core.Extensions;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aurem.Syncing.Internals;
using Aurem.Syncing.Internals.Packets.Bodies;
using System.Security.Cryptography;
using Aurem.Units;
using Aurem.Syncing.Internals.Packets;

namespace Aurem.Syncing
{
    public class MulticastServer : IService
    {
        public const int OutPoolSize = 1;
        public const int InPoolSize = 1;

        protected ushort Pid;
        protected ushort NProc;
        protected IOrderer Orderer;
        protected Network Netserv;
        protected Channel<MCastRequest>[] Requests;
        protected CancellationTokenSource StopOut;
        protected Logger Log;

        protected MulticastServer(Config.Config conf, IOrderer orderer, Network network, Logger log)
        {
            Requests = new Channel<MCastRequest>[conf.NProc];
            for (int i = 0; i < conf.NProc; i++)
            {
                Requests[i] = Channel.CreateBounded<MCastRequest>(conf.EpochLength);
            }

            Pid = conf.Pid;
            NProc = conf.NProc;
            Orderer = orderer;
            Netserv = network;
            StopOut = new CancellationTokenSource();
            Log = log;

            network.AddHandle(In);
        }

        public static (IService, Requests.Multicast) NewServer(Config.Config conf, IOrderer orderer, Network network, Logger log)
        {
            var s = new MulticastServer(conf, orderer, network, log);
            return (s, s.Send);
        }

        public async Task<Exception?> Start()
        {
            await Netserv.Start();
            return null;
        }

        public async Task StopAsync()
        {
            StopOut.Cancel();
        }

        protected virtual async Task Send(IUnit u)
        {
            if (u.Creator() != Pid)
            {
                throw new Exception("Attempting to multicast unit that we didn't create");
            }

            var encUnit = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.EncodeUnit, u, out var err);
            if (err != null)
            {
                Log.Error().Str("where", "MulticastServer.Send.EncodeUnit").Msg(err.Message);
                return;
            }


            var indices = ShuffleOrder(NProc);
            foreach (var idx in indices)
            {
                if (idx == Pid) continue;
                Netserv.Send((ushort)idx, new Packet(PacketID.MCASTSEND, new MCastSendUnit(u)));
                Log.Info().Val(Logging.Constants.Height, u.Height()).Val(Logging.Constants.PID, idx).Msg(Logging.Constants.SentUnit);
                //await Requests[idx].Writer.WriteAsync(new MCastRequest { EncodedUnit = encUnit, Height = u.Height() }); // unlikely to block for long, if at all
            }
        }

        public static int[] ShuffleOrder(ushort nproc, int seed = -1)
        {
            // Fisher-Yates shuffle
            var indices = Enumerable.Range(0, nproc).ToArray();
            var rng = (seed == -1) ? new System.Random() : new System.Random(seed);
            for (int i = 0; i <= indices.Length - 2; i++)
            {
                int j = i + rng.Next(indices.Length - i);
                (indices[j], indices[i]) = (indices[i], indices[j]);
            }

            return indices;
        }

        protected virtual async Task In(Packet p)
        {
            if (p.Body is MCastSendUnit m)
            {
                try
                {
                    LoggingUtil.AddingErrors(await Orderer.AddPreunits(m.Unit.Creator(), m.Unit), 1, Log);
                }
                finally
                {
                }
            }
            else
            {
                Log.Error().Msg("received wrong packet type");
            }
        }
    }
}
