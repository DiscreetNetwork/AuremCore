using BN256Core;
using BN256Core.Extensions;
using AuremCore.Crypto.Multi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using BN256Core.Common;
using AuremCore.Crypto.Threshold;

namespace AuremCore.RMC
{
    public class Instance
    {
        protected SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);
        
        public ulong Id;
        public Keychain Keys;
        public MultiSignature Proof;

        protected uint RawLength;
        protected byte[] SignedData;
        protected Status Stat;

        public Instance() { }

        public static Instance NewOutgoing(ulong id, byte[] data, Keychain keys)
        {
            var rawLen = (uint)data.Length;
            var buf = new byte[8 + rawLen];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, id);
            Buffer.BlockCopy(buf, 8, data, 0, data.Length);
            var signedData = buf.Concat(keys.Sign(buf));
            var nproc = (ushort)keys.Length;
            var proof = new MultiSignature(TUtil.MinimalQuorum(nproc), signedData);
            proof.Aggregate(keys.Pid(), keys.Sign(signedData));

            return new Instance
            {
                Id = id,
                Keys = keys,
                RawLength = rawLen,
                SignedData = signedData,
                Proof = proof,
                Stat = Status.Data
            };
        }

        public static Instance NewRaw(ulong id, byte[] data, Keychain keys)
        {
            var rawLen = (uint)data.Length;
            var nproc = (ushort)keys.Length;
            var proof = new MultiSignature(TUtil.MinimalQuorum(nproc), data);
            proof.Aggregate(keys.Pid(), keys.Sign(data));

            return new Instance
            {
                Id = id,
                Keys = keys,
                RawLength = rawLen,
                SignedData = data,
                Proof = proof,
                Stat = Status.Data
            };
        }

        public async Task<Exception?> SendData(Stream s)
        {
            await Mutex.WaitAsync();

            try
            {
                var fullBuf = new byte[4 + SignedData.Length];
                BinaryPrimitives.WriteUInt32LittleEndian(fullBuf, RawLength);
                Array.Copy(SignedData, 0, fullBuf, 4, SignedData.Length);

                await s.WriteAsync(fullBuf);

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                Mutex.Release();
            }
        }

        public async Task<Exception?> SendProof(Stream s)
        {
            await Mutex.WaitAsync();

            try
            {
                if (Stat != Status.Finished)
                {
                    throw new Exception("no proof to send");
                }

                await s.WriteAsync(Proof.Marshal());

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                Mutex.Release();
            }
        }

        public async Task<Exception?> SendFinished(Stream s)
        {
            try
            {
                await SendData(s);
                await SendProof(s);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        public async Task<(bool, Exception?)> AcceptSignature(ushort pid, Stream s)
        {
            var signature = new byte[Keychain.SignatureLength];
            try
            {
                await s.ReadAsync(signature);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }

            await Mutex.WaitAsync();

            try
            {
                if (Keys.Verify(pid, SignedData.Concat(signature).ToArray()))
                {
                    return (false, new Exception("wrong signature"));
                }

                if (Stat != Status.Finished)
                {
                    (var done, var err) = Proof.Aggregate(pid, signature);
                    if (done)
                    {
                        Stat = Status.Finished;
                        return (true, err);
                    }

                    return (false, err);
                }

                return (false, null);
            }
            finally
            {
                Mutex.Release();
            }
        }

        public async Task<Exception?> SendSignature(Stream s)
        {
            await Mutex.WaitAsync();

            try
            {
                if (Stat == Status.Unknown)
                {
                    return new Exception("cannot signed unknown data");
                }

                var sig = Keys.Sign(SignedData);
                
                await s.WriteAsync(sig);

                if (Stat == Status.Data)
                {
                    Stat = Status.Signed;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                Mutex.Release();
            }
        }

        public async Task<Exception?> AcceptProof(Stream s)
        {
            await Mutex.WaitAsync();

            try
            {
                if (Stat == Status.Unknown)
                {
                    throw new Exception("cannot accept proof of unknown data");
                }

                var nproc = (ushort)Keys.Length;
                var proof = new MultiSignature(TUtil.MinimalQuorum(nproc), SignedData);
                var data = new byte[proof.Length];

                int _dataRead = await s.ReadAsync(data);

                if (_dataRead != data.Length)
                {
                    throw new Exception("received less than the expected number of bytes");
                }

                proof.Unmarshal(data);

                if (!Keys.MultiVerify(proof))
                {
                    return new Exception("wrong multisignature");
                }

                if (Stat != Status.Finished)
                {
                    Proof = proof;
                    Stat = Status.Finished;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                Mutex.Release();
            }
        }

        public byte[] Data()
        {
            if (RawLength == SignedData.Length) return SignedData;

            return SignedData[8..(int)(8 + RawLength)];
        }

        public Status GetStatus()
        {
            Mutex.Wait();

            try
            {
                return Stat;
            }
            finally
            {
                Mutex.Release();
            }
        }
    }
}
