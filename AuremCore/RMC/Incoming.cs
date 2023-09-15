using AuremCore.Crypto.Multi;
using AuremCore.Crypto.Threshold;
using BN256Core.Extensions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    public class Incoming : Instance
    {
        public ushort Pid;

        public Incoming(ulong id, ushort pid, Keychain keys)
        {
            Id = id;
            Keys = keys;
            Pid = pid;
        }

        public async Task<(byte[], Exception?)> AcceptData(Stream s)
        {
            (var rawLen, var err) = await DecodeUint32(s);
            if (err != null)
            {
                return (Array.Empty<byte>(), err);
            }

            var signedData = new byte[8 + rawLen + Keychain.SignatureLength];

            try
            {
                var _signedDataRead = await s.ReadAsync(signedData);
                if (_signedDataRead != signedData.Length)
                {
                    return (Array.Empty<byte>(), new Exception("did not receive correct length of data"));
                }

                var id = BinaryPrimitives.ReadUInt64LittleEndian(signedData);
                if (id != Id)
                {
                    return (Array.Empty<byte>(), new Exception("incoming id mismatch"));
                }

                if (!Keys.Verify(Pid, signedData))
                {
                    return (Array.Empty<byte>(), new Exception("wrong data signature"));
                }

                var nproc = (ushort)Keys.Length;
                var proof = new MultiSignature(TUtil.MinimalQuorum(nproc), signedData);

                await Mutex.WaitAsync();

                try
                {
                    if (Stat == Status.Unknown)
                    {
                        Stat = Status.Data;
                    }
                    else
                    {
                        var thisData = signedData[8..(int)(8 + rawLen)];
                        if (!thisData.BEquals(Data()))
                        {
                            return (Array.Empty<byte>(), new Exception("different data already accepted"));
                        }

                        return (Data(), null);
                    }

                    SignedData = signedData;
                    RawLength = rawLen;
                    Proof = proof;

                    return (Data(), null);
                }
                catch (Exception ex)
                {
                    return (Array.Empty<byte>(), ex);
                }
                finally
                {
                    Mutex.Release();
                }
            }
            catch (Exception ex)
            {
                return (Array.Empty<byte>(), ex);
            }
        }

        public async Task<(byte[], Exception?)> AcceptFinished(Stream s)
        {
            (var res, var err) = await AcceptData(s);
            if (err != null) return (res, err);

            return (res, await AcceptProof(s));
        }

        public static async Task<(uint, Exception?)> DecodeUint32(Stream s)
        {
            try
            {
                var buf = new byte[4];
                await s.ReadAsync(buf);

                return (BinaryPrimitives.ReadUInt32LittleEndian(buf), null);
            }
            catch (Exception ex)
            {
                return (0, ex);
            }
        }

        public static async Task<Exception?> EncodeUint32(Stream s, uint i)
        {
            try
            {
                var buf = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(buf, i);

                await s.WriteAsync(buf);

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
