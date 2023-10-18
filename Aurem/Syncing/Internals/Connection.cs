using Aurem.Syncing.Internals.Packets;
using AuremCore.Crypto.P2P;
using BN256Core;
using BN256Core.Extensions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals
{
    public class Connection
    {
        private const int ChallengeLength = 32;

        public IPEndPoint Receiver { get; private set; }

        private TcpClient _tcpClient;
        private Network _network;
        private CancellationTokenSource _cts;
        private Channel<Packet> _packetQueue;
        private TimeSpan _timeout;
        private P2PSecretKey? _key;
        private List<P2PPublicKey>? _theirKeys;
        private ulong _verified;

        public Connection(TcpClient tcpClient, TimeSpan timeout, P2PSecretKey? key, List<P2PPublicKey>? theirKeys, Network network)
        {
            _tcpClient = tcpClient;
            _timeout = timeout;
            _network = network;
            _key = key;
            _theirKeys = theirKeys;
            Receiver = _tcpClient.Client.RemoteEndPoint != null ? (IPEndPoint)_tcpClient.Client.RemoteEndPoint : IPEndPoint.Parse("0.0.0.0:0");
            _packetQueue = Channel.CreateUnbounded<Packet>();
            _cts = new CancellationTokenSource();
            _verified = 0;
        }

        /// <summary>
        /// Attempts to make a handshake with the connection given our node's PID. Returns true if the handshake was successful.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="TimeoutException"></exception>
        public async Task<bool> SendHandshake(ushort pid)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            if (_key == null) throw new Exception("p2pkey was null");

            // 1. Accept challenge
            var challenge = new byte[ChallengeLength];
            await _tcpClient.GetStream().ReadAsync(challenge, cts.Token);
            if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new TimeoutException("connection read timed out");
            }
            else if (_cts.IsCancellationRequested)
            {
                throw new Exception("connection has been cancelled");
            }

            // 2. Sign challenge
            var challengeSig = _key.Sign(challenge).Marshal();
            var px = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(px, pid);
            var response = challengeSig.Concat(px);
            
            // 3. Send signed challenge (response) and our PID
            await _tcpClient.GetStream().WriteAsync(response, cts.Token);
            if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new TimeoutException("connection write timed out");
            }
            else if (_cts.IsCancellationRequested)
            {
                throw new Exception("connection has been cancelled");
            }

            // 4. Acknowledge the connection
            var ack = new byte[1];
            await _tcpClient.GetStream().ReadAsync(ack, cts.Token);

            if (ack[0] == 0x01)
            {
                Acknowledge();
                cts.Dispose();

                return true;
            }
            else if (ack[0] == 0xff)
            {
                // the connection was a duplicate to an already incoming connection
                cts.Token.Register(() => _tcpClient.Dispose());
                return false;
            }
            else
            {
                // unknown invalid ack byte
                throw new Exception($"Unknown ACK byte: {ack[0]}");
            }
        }

        public async Task<ushort> AcceptHandshake()
        {
            var challenge = new byte[ChallengeLength];
            var rng = new System.Random();
            rng.NextBytes(challenge);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            // 1. Send challenge
            await _tcpClient.GetStream().WriteAsync(challenge, cts.Token);
            if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new TimeoutException("connection write timed out");
            }
            else if (_cts.IsCancellationRequested)
            {
                throw new Exception("connection has been cancelled");
            }

            // 2. Read challenge response + PID
            var response = new byte[Constants.SignatureLength + 2];
            await _tcpClient.GetStream().ReadAsync(response, cts.Token);
            if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new TimeoutException("connection read timed out");
            }
            else if (_cts.IsCancellationRequested)
            {
                throw new Exception("connection has been cancelled");
            }
            
            // 3. Verify the response
            var challengeSig = new Signature().Unmarshal(response.AsSpan(0));
            var pid = BinaryPrimitives.ReadUInt16LittleEndian(response.AsSpan(Constants.SignatureLength));
            if (_theirKeys == null || _theirKeys.Count <= pid || _theirKeys[pid] == null)
            {
                throw new Exception("could not find their p2p key with given pid");
            }

            if (!_theirKeys[pid].Verify(challenge, challengeSig))
            {
                throw new Exception("connection verification failed");
            }

            cts.Dispose();
            return pid;
        }

        /// <summary>
        /// This is called when a connection is already established and this new connection isn't needed.
        /// </summary>
        /// <returns></returns>
        public async Task Disregard()
        {
            await _tcpClient.GetStream().WriteAsync(new byte[] { 0xff });
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            cts.Token.Register(() => _tcpClient.Dispose());
        }

        public async Task Heed()
        {
            // 4. Acknowledge the connection
            await _tcpClient.GetStream().WriteAsync(new byte[] { 0x01 });
            Acknowledge();
        }

        public bool Acknowledged { get
            {
                return Interlocked.Read(ref _verified) > 0;
            } 
        }

        public void Acknowledge()
        {
            Interlocked.Increment(ref _verified);
        }

        public bool DataAvailable => _tcpClient.GetStream().DataAvailable;

        public void Stop()
        {
            _cts.Cancel();
            // TODO: disconnect gracefully on stop with timer
        }

        internal bool OptimisticTryGetPacket([NotNullWhen(true)] out Packet? packet)
        {
            return _packetQueue.Reader.TryRead(out packet);
        }

        internal async Task SendAll(Packet? p = null)
        {
            if (p != null) await SendAsync(p);
            while (_packetQueue.Reader.TryRead(out var packet) && !_cts.IsCancellationRequested)
            {
                await SendAsync(packet);
            }
        }

        internal void Send(Packet p)
        {
            _packetQueue.Writer.TryWrite(p);
        }

        internal async Task SendAsync(Packet p)
        {
            //await Console.Out.WriteLineAsync($"sending packet {(PacketID)p.Header.PacketID}");
            if (p != null)
            {
                var dataStream = new MemoryStream();
                p.Serialize(dataStream);
                var ps = dataStream.ToArray();

                var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                cts.CancelAfter(_timeout);
                await _tcpClient.GetStream().WriteAsync(ps, cts.Token);
                if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
                {
                    throw new Exception("Send operation on connection timed out.");
                }
                else if (_cts.IsCancellationRequested)
                {
                    // Stop() was called
                    return;
                }

                cts.Dispose();
            }
        }

        internal async Task<Packet?> Receive()
        {
            int numBytesRead = 0;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(_timeout);

            var headerBytes = new byte[PacketHeader.StaticSize];
            numBytesRead += await _tcpClient.GetStream().ReadAsync(headerBytes, cts.Token);

            if (numBytesRead != PacketHeader.StaticSize)
            {
                throw new Exception("Did not read entire header; invalid data");
            }
            else if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new Exception("Receive operation on connection timed out.");
            }
            else if (_cts.IsCancellationRequested)
            {
                // Stop() was called
                return null;
            }

            PacketHeader header = new PacketHeader(headerBytes);
            if (header.NetworkID != PacketHeader.DefaultNetworkID)
            {
                throw new Exception($"Network ID invalid; expected {PacketHeader.DefaultNetworkID} but got {header.NetworkID}");
            }

            byte[] data = new byte[header.Length];
            numBytesRead = 0;
            while (numBytesRead < header.Length && !cts.IsCancellationRequested)
            {
                numBytesRead += await _tcpClient.GetStream().ReadAsync(data.AsMemory(numBytesRead, data.Length - numBytesRead), cts.Token);
            }

            
            if (cts.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                throw new Exception("Receive operation on connection timed out.");
            }
            else if (_cts.IsCancellationRequested)
            {
                // Stop() was called
                return null;
            }
            else if(numBytesRead != header.Length)
            {
                throw new Exception("Did not read entire header; invalid data");
            }
            
            cts.Dispose();

            var pb = Packet.DecodePacketBody((PacketID)header.PacketID, data);
            var p = new Packet(header, pb);

            // verify the packet checksum
            var chk = BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(SHA256.HashData(data)));
            if (chk != header.Checksum)
            {
                throw new Exception($"Invalid checksum: calculated {chk} but was told to expect {header.Checksum}");
            }

            return p;
        }

        public async Task ReceiveAll()
        {
            while (DataAvailable && !_cts.IsCancellationRequested)
            {
                var p = await Receive();
                if (p != null)
                {
                    if (_network.OnReceive != null)
                    {
                        await _network.OnReceive(p);
                    }
                }
            }
        }
    }
}
