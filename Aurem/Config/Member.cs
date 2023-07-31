using Aurem.Model;
using Aurem.Config.Extensions;
using AuremCore.Crypto.P2P;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurem.Crypto.Signing;

namespace Aurem.Config
{
    /// <summary>
    /// Represents the private data about a committee member.
    /// </summary>
    public class Member
    {
        /// <summary>
        /// The process ID of this member.
        /// </summary>
        public ushort Pid { get; set; }

        /// <summary>
        /// The private key of this committee member.
        /// </summary>
        public IPrivateKey PrivateKey { get; set; }

        /// <summary>
        /// The secret key this committe member uses for RMC.
        /// </summary>
        public SecretKey RMCSecretKey { get; set; }

        /// <summary>
        /// The key for generating P2P Public keys for P2P communication.
        /// </summary>
        public P2PSecretKey P2PSecretKey { get; set; }

        private static readonly string MalformedData = "malformed committee data";

        public static Member LoadMember(Stream s)
        {
            StreamReader sr = new(s);
            
            var str = sr.ReadWord();
            if (str == null) throw new EndOfStreamException(MalformedData);
            var privateKey = SPrivateKey.DecodePrivateKey(str);

            str = sr.ReadWord();
            if (str == null) throw new EndOfStreamException(MalformedData);
            var secretKey = SecretKey.DecodeSecretKey(str);

            str = sr.ReadWord();
            if (str == null) throw new EndOfStreamException(MalformedData);
            var sKey = P2PSecretKey.Decode(str);

            str = sr.ReadWord();
            if (str == null) throw new EndOfStreamException(MalformedData);
            var pid = ushort.Parse(str);

            return new Member { PrivateKey = privateKey, RMCSecretKey = secretKey, P2PSecretKey = sKey, Pid = pid };
        }

        public void StoreMember(Stream s)
        {
            StreamWriter w = new StreamWriter(s);
            w.Write(PrivateKey.Encode() + " ");
            w.Write(RMCSecretKey.Encode() + " ");
            w.Write(P2PSecretKey.Encode() + " ");
            w.Write(Pid.ToString() + "\n");
        }
    }
}
