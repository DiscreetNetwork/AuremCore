using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Packets
{
    public class RegisterPacket
    {
        private ushort[] ports;

        public IPAddress Address { get; set; }

        public RegisterPacket(ReadOnlySpan<byte> data, bool forRegistry = false)
        {
            if (forRegistry) DeserializeForRegistry(data);
            else Deserialize(data);
        }

        public static int Size => 16;

        public static int RegistrySize => 32;

        public ushort[] GetPorts() => ports;

        public RegisterPacket(ushort[] ports)
        {
            if (ports == null || ports.Length != 8) throw new ArgumentException(nameof(ports));
            this.ports = ports;
        }

        public int SetupRmcPort => ports[0];
        public int SetupFetchPort => ports[1];
        public int SetupGossipPort => ports[2];

        public int RmcPort => ports[3];
        public int McastPort => ports[4];
        public int FetchPort => ports[5];
        public int GossipPort => ports[6];

        public int SetupCommitteePort => ports[7];

        public byte[] Serialize()
        {
            byte[] data = new byte[ports.Length * 2];
            for (int i = 0; i < ports.Length; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2 * i), ports[i]);
            }

            return data;
        }

        public byte[] SerializeForRegistry()
        {
            byte[] data = new byte[ports.Length * 2 + 16];
            if (Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                Array.Copy(Address.GetAddressBytes(), data, 16);
            }
            else
            {
                data[10] = 0xff;
                data[11] = 0xff;
                Array.Copy(Address.GetAddressBytes(), 0, data, 12, 4);
            }

            for (int i = 0; i < ports.Length; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(16 + 2 * i), ports[i]);
            }

            return data;
        }

        public static byte[] SerializeIP(IPAddress ip)
        {
            var data = new byte[16];
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                Array.Copy(ip.GetAddressBytes(), data, 16);
            }
            else
            {
                data[10] = 0xff;
                data[11] = 0xff;
                Array.Copy(ip.GetAddressBytes(), 0, data, 12, 4);
            }

            return data;
        }

        public static IPAddress DeserializeIP(ReadOnlySpan<byte> data)
        {
            IPAddress ip;
            if (IsIPv4(data))
            {
                ip = new IPAddress(data.Slice(12, 4));
            }
            else
            {
                ip = new IPAddress(data.Slice(0, 16));
            }

            return ip;
        }

        private static bool IsIPv4(ReadOnlySpan<byte> b)
        {
            bool _isIPv4 = b[0] == 0 && b[1] == 0 && b[2] == 0 && b[3] == 0 && b[4] == 0 && b[5] == 0 && b[6] == 0 && b[7] == 0 && b[8] == 0 && b[9] == 0 && b[10] == 0xff && b[11] == 0xff;

            return _isIPv4;
        }

        public void DeserializeForRegistry(ReadOnlySpan<byte> data)
        {
            if (data.Length < 32) throw new ArgumentException(nameof(data));

            if (IsIPv4(data))
            {
                Address = new IPAddress(data.Slice(12, 4));
            }
            else
            {
                Address = new IPAddress(data.Slice(0, 16));
            }

            ports = new ushort[8];
            for(int i = 0; i < ports.Length; i++)
            {
                ports[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(16 + 2 * i));
            }
        }

        public void Deserialize(ReadOnlySpan<byte> data)
        {
            ports = new ushort[8];
            if (data.Length < 16) throw new ArgumentException(nameof(data));

            for (int i = 0; i < ports.Length; i++)
            {
                ports[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2 * i));
            }
        }
    }
}