using Aurem.Crypto.Signing;
using Aurem.Model;
using AuremCore.Crypto.P2P;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Packets
{
    public class KeyMaterialPacket
    {
        public IPublicKey PublicKey { get; set; }
        public IPrivateKey? PrivateKey { get; set; }
        public SecretKey? SecKey { get; set; }
        public VerificationKey VerKey { get; set; }
        public P2PPublicKey P2PPubKey { get; set; }
        public P2PSecretKey? P2PSecKey { get; set; }

        public KeyMaterialPacket()
        {
            (PublicKey, PrivateKey) = Keys.GenerateKeys();
            (VerKey, SecKey) = VerificationKey.GenerateKeys();
            (P2PPubKey, P2PSecKey) = P2P.GenerateKeys();
        }

        public KeyMaterialPacket(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public static int Size => 32 + 2 * 129 + 64;

        public byte[] Serialize()
        {
            var bytes = new byte[Size];
            PublicKey.Serialize().AsSpan().CopyTo(bytes);
            VerKey.Marshal().AsSpan().CopyTo(bytes.AsSpan(32, 129));
            P2PPubKey.Marshal().AsSpan().CopyTo(bytes.AsSpan(32 + 129));

            return bytes;
        }

        public void Deserialize(ReadOnlySpan<byte> bytes)
        {
            PublicKey = new SPublicKey(bytes);
            bytes = bytes.Slice(32);

            VerKey = new VerificationKey().Unmarshal(bytes);
            bytes = bytes.Slice(129);

            P2PPubKey = new P2PPublicKey().Unmarshal(bytes.ToArray());
        }

        public static (Dictionary<string, List<string>> setup, Dictionary<string, List<string>> addrs) MakeAddressMap(List<(RegisterPacket, KeyMaterialPacket)> ps)
        {
            var setupAddresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "fetch", new() },
                { "gossip", new() }
            };
            var addresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "mcast", new() },
                { "fetch", new() },
                { "gossip", new() }
            };

            // TODO: make map

            return (setupAddresses, addresses);
        }
    }
}
