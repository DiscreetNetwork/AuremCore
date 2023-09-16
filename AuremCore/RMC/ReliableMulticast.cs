using AuremCore.Crypto.Multi;
using BN256Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    // TODO: documentation
    public class ReliableMulticast
    {
        private SemaphoreSlim _inMx = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _outMx = new SemaphoreSlim(1, 1);

        private Keychain Keys;
        private ConcurrentDictionary<ulong, Incoming> In;
        private ConcurrentDictionary<ulong, Instance> Out;

        public ReliableMulticast(IEnumerable<VerificationKey> pubs, SecretKey priv)
        {
            Keys = new Keychain(pubs, priv);
            In = new();
            Out = new();
        }

        public (Instance, Exception?) Get(ulong id)
        {
            (var incoming, var err) = GetIn(id);
            if (err == null)
            {
                return (incoming, null);
            }

            (var outgoing, err) = GetOut(id);
            if (err == null)
            {
                return (outgoing, null);
            }

            return (null!, new Exception("unknown instance"));
        }

        public (Incoming, Exception?) GetIn(ulong id)
        {
            var success = In.TryGetValue(id, out var incoming);
            if (success)
            {
                return (incoming!, null);
            }

            return (null!, new Exception("unknown incoming"));
        }

        public (Instance, Exception?) GetOut(ulong id)
        {
            var success = Out.TryGetValue(id, out var outgoing);
            if (success)
            {
                return (outgoing!, null);
            }

            return (null!, new Exception("unknown outgoing"));
        }

        public (Instance, Exception?) NewRawInstance(ulong id, byte[] data)
        {
            var res = Instance.NewRaw(id, data, Keys);

            var success = Out.AddOrUpdate(id, res, (a, x) => x);
            return (success, success != res ? new Exception("duplicate raw") : null);
        }

        public Instance NewOutgoingInstance(ulong id, byte[] data)
        {
            var res = Instance.NewOutgoing(id, data, Keys);

            return Out.AddOrUpdate(id, res, (a, x) => x);
        }

        public (Incoming, Exception?) NewIncomingInstance(ulong id, ushort pid)
        {
            var res = new Incoming(id, pid, Keys);

            var success = In.AddOrUpdate(id, res, (a, x) => x);
            return (success, success != res ? new Exception("duplicate incoming") : null);
        }

        public void Clear(ulong id)
        {
            In.TryRemove(id, out _);
            Out.TryRemove(id, out _);
        }

        public MultiSignature? Proof(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return null;
            return ins.Proof;
        }

        public byte[] Data(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return Array.Empty<byte>();
            return ins.Data();
        }

        public Status Status(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return RMC.Status.Unknown;
            return ins.GetStatus();
        }

        public async Task<(byte[], Exception?)> AcceptFinished(ulong id, ushort pid, Stream s)
        {
            (var ins, var err) = GetIn(id);
            if (err != null)
            {
                (ins, _) = NewIncomingInstance(id, pid);
            }

            return await ins.AcceptFinished(s);
        }

        public async Task<Exception?> SendFinished(ulong id, Stream s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return err;
            }

            return await ins.SendFinished(s);
        }

        public async Task<Exception?> SendProof(ulong id, Stream s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return err;
            }

            return await ins.SendProof(s);
        }

        public async Task<(bool, Exception?)> AcceptSignature(ulong id, ushort pid, Stream s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return (false, err);
            }

            return await ins.AcceptSignature(pid, s);
        }

        public async Task<Exception?> SendData(ulong id, byte[] data, Stream s)
        {
            if (Status(id) != RMC.Status.Unknown)
            {
                (var outs, var err) = GetOut(id);
                if (err != null)
                {
                    return err;
                }

                return await outs.SendData(s);
            }
            else
            {
                var outs = NewOutgoingInstance(id, data);
                return await outs.SendData(s);
            }
        }

        public async Task<Exception?> AcceptProof(ulong id, Stream s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return err;
            }

            return await ins.AcceptProof(s);
        }

        public async Task<Exception?> SendSignature(ulong id, Stream s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return err;
            }

            return await ins.SendSignature(s);
        }

        public async Task<(byte[], Exception?)> AcceptData(ulong id, ushort pid, Stream s)
        {
            (var ins, var err) = GetIn(id);
            if (err != null)
            {
                (ins, err) = NewIncomingInstance(id, pid);
            }

            if (err != null)
            {
                return (Array.Empty<byte>(), err);
            }

            return await ins.AcceptData(s);
        }

        public Exception? InitiateRaw(ulong id, byte[] data)
        {
            (_, var err) = NewRawInstance(id, data);
            return err;
        }
    }
}
