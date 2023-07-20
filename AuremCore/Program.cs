using System;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using BN256Core;
using BN256Core.Common;
using BN256Core.Models;
using BN256Core.Native;
using AuremCore.Crypto.Encrypt;
using AuremCore.Crypto.P2P;
using AuremCore.Crypto.Threshold;

namespace AuremCore
{
    public class Program
    {
        public static void TestRSA()
        {
            (var enc, var dec) = RSA.GenerateKeys();
            var encpub = enc.Encode();
            Console.WriteLine(encpub);
            var decpub = dec.Encode();
            Console.WriteLine(decpub);
            var enc2 = EncryptionKey.Decode(encpub);
            var dec2 = DecryptionKey.Decode(decpub);
            var plain = Encoding.ASCII.GetBytes("this is a test string intended for rsa testing");
            var cipher = enc2.Encrypt(plain);
            Console.WriteLine(PrintUtil.Hexify(cipher, true));
            var recovered = dec2.Decrypt(cipher);
            Console.WriteLine(Encoding.ASCII.GetString(recovered));
            Console.WriteLine(" LINEBREAK ");
        }
        
        private class Node
        {
            internal SecretKey secretComms;
            internal P2PPublicKey publicKey;

            internal ushort pid;

            internal ThresholdKey threshold;
            internal Share share;
            internal Signature sig;

            // comms
            internal SymmetricKey[] commKeys;
            internal P2PPublicKey[] commPubs;

            public void GetComms(IEnumerable<P2PPublicKey> keys)
            {
                commPubs = keys.ToArray();
                commKeys = P2P.Keys(new P2PSecretKey(secretComms), commPubs, pid);
            }
        }

        private static Random rng_Shuffle = new Random();

        public static IEnumerable<T> Shuffle<T>(IEnumerable<T> values)
        {
            var list = new List<T>(values);
            var n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng_Shuffle.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return values;
        }

        public static void TestTSS(ushort nproc)
        {
            // TSS = Threshold Signature Scheme
            //ushort nproc = 50;
            nproc = 3;
            ushort threshold = TUtil.MinimalQuorum(nproc);
            threshold = 2;
            TSS tss = TSS.CreateSeeded(nproc, threshold, 0, out var tk_0);
            Node[] nodes = new Node[nproc];
            //var secretDealerKey = new SecretKey(SecretKey.RandomScalar());
            //Dealer dealer = new Dealer { secretComms = secretDealerKey, publicKey = new P2PPublicKey(secretDealerKey), pid = 0 };
            // instantiate nodes
            for (int i = 0; i < nproc; i++)
            {
                var skey = new SecretKey(SecretKey.RandomScalar());
                nodes[i] = new Node { secretComms = skey, publicKey = new P2PPublicKey(skey), pid = (ushort)(i) };
            }
            var nkeys = nodes.Select(x => x.publicKey);

            // set up communications
            //dealer.GetComms(nkeys);
            for (int i = 0; i < nproc; i++)
            {
                nodes[i].GetComms(nkeys);
            }

            // get threshold keys
            for (int i = 0; i < nproc; i++)
            {
                nodes[i].threshold = tss.Encrypt(nodes[0].commKeys);
            }
            for (int i = 0; i < nproc; i++)
            {
                nodes[i].threshold.owner = nodes[i].pid;
                nodes[i].threshold.dealer = nodes[0].pid;
                nodes[i].threshold.threshold = threshold;
            }

            // decode secret keys
            for (int i = 0; i < nproc; i++)
            {
                var success = nodes[i].threshold.CheckSecretKey(nodes[i].pid, nodes[i].commKeys[0]);
                if (!success) throw new Exception("invalid key");
            }

            var msg = Encoding.ASCII.GetBytes("this is a message intended to be signed by the threshold committee");
            // create shares
            List<Share> sharesToCommunicate = new List<Share>();
            for (int i = 1; i < nproc; i++)
            {
                nodes[i].share = nodes[i].threshold.CreateShare(msg);
                sharesToCommunicate.Add(nodes[i].share);
            }

            List<Signature> thresholdSigs = new();
            for (int i = 1; i < nproc; i++)
            {
                nodes[i].sig = nodes[i].threshold.CombineShares(sharesToCommunicate.ToArray()).Item1 ?? new Signature();
                thresholdSigs.Add(nodes[i].sig);
                Console.WriteLine("calculated sig: " + PrintUtil.Hexify(nodes[i].sig.Marshal(), true));
                Console.WriteLine("Should've been: " + PrintUtil.Hexify(new SecretKey(tk_0).Sign(msg).Marshal(), true));
                if (!nodes[i].threshold.VerifySignature(nodes[i].sig, msg)) return;//throw new Exception("invalid sig!");
            }

            //for (int i = 0; i < nproc - 1; i++)
            //{
            //    Console.WriteLine(PrintUtil.Hexify(thresholdSigs[i].Marshal(), true));
            //}
        }

        public static void TestThreshold(int n, int tot, bool checkAll = false)
        {
            var t = TUtil.MinimalQuorum((ushort)n);

            var sKeys = new P2PSecretKey[n];
            var pKeys = new P2PPublicKey[n];
            var p2pKeys = new SymmetricKey[n][];

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < n; i++)
            {
                (pKeys[i], sKeys[i]) = P2P.GenerateKeys();
            }

            for (int i = 0; i < n; i++)
            {
                p2pKeys[i] = P2P.Keys(sKeys[i], pKeys, (ushort)i);
            }

            for (int tcnt = 0; tcnt < tot; tcnt++)
            {
                var tcs = new ThresholdKey[n];
                var gtc = TSS.CreateSeeded((ushort)n, (ushort)t, 0, out var tk_0);
                var dealer = n - 1;

                var tc = gtc.Encrypt(p2pKeys[dealer]);
                var tcEncoded = tc.Encode();

                for (ushort i = 0; i < n; i++)
                {
                    (tcs[i], _) = ThresholdKey.Decode(tcEncoded, (ushort)dealer, i, p2pKeys[i][dealer]);
                    //Console.WriteLine($"globalVK: {PrintUtil.Hexify(tcs[i].globalVK.Marshal(), true)}");
                }

                var msg = Encoding.ASCII.GetBytes("this is a message intended to be signed by the threshold committee");
                var shares = new Share[n];
                for (ushort i = 0; i < n; i++)
                {
                    shares[i] = tcs[i].CreateShare(msg);
                }

                if (checkAll)
                {
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            if (!tcs[i].VerifyShare(shares[j], msg)) throw new Exception("should be verified correctly");
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (!tcs[0].VerifyShare(shares[j], msg)) throw new Exception("should be verified correctly");
                    }
                }
                

                //for (int i = 0; i < n; i++)
                //{
                //    (var c, var ok) = tcs[0].CombineShares(shares);
                //    //Console.WriteLine(PrintUtil.Hexify(c.Marshal(), true));
                //    //Console.WriteLine(PrintUtil.Hexify(new SecretKey(tk_0).Sign(msg).Marshal(), true));
                //    if (!ok) throw new Exception("should be correctly combined by t-parties");
                //    if (!tcs[0].VerifySignature(c, msg)) throw new Exception("signature should pass");
                //}

                if (checkAll)
                {
                    for (int i = 0; i < n; i++)
                    {
                        var permShares = Shuffle(shares).ToArray();
                        (var c, var ok) = tcs[i].CombineShares(permShares);
                        //Console.WriteLine(PrintUtil.Hexify(c.Marshal(), true));
                        //Console.WriteLine(PrintUtil.Hexify(new SecretKey(tk_0).Sign(msg).Marshal(), true));
                        if (!ok) throw new Exception("should be correctly combined by t-parties");
                        if (!tcs[0].VerifySignature(c, msg)) throw new Exception("signature should pass");
                    }
                }
                else
                {
                    var permShares = Shuffle(shares).ToArray();
                    (var c, var ok) = tcs[0].CombineShares(permShares);

                    //Console.WriteLine(c.Sig.p.ToString());
                    //Console.WriteLine(new SecretKey(tk_0).Sign(msg).Sig.p.ToString());

                    //Console.WriteLine(PrintUtil.Hexify(c.Marshal(), true));
                    //Console.WriteLine(PrintUtil.Hexify(new SecretKey(tk_0).Sign(msg).Marshal(), true));

                    if (!ok) throw new Exception("should be correctly combined by t-parties");
                    if (!tcs[0].VerifySignature(c, msg)) throw new Exception("signature should pass");
                }
            }

            sw.Stop();

            Console.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Time per TSS: {(double)sw.ElapsedMilliseconds / (double)tot}ms");
        }

        public static void Main(string[] args)
        {
            //TestRSA();

            var numtss = 100;
            var nproc = 100;
            //Stopwatch sw = Stopwatch.StartNew();
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine(TUtil.Lagrange(Enumerable.Range(0, 10).Select(x => (long)x).ToArray(), (long)i));
            
            //}
            TestThreshold(nproc, numtss);
            //for (int i = 0; i < numtss; i++) TestThreshold(nproc);

            BN a = new BN();
            BigInteger c = new BigInteger();

           
            //c = new BigInteger(252);
            //BN.ToBN(c, a.array);
            //var d = BN.FromBN(a);
            //Console.WriteLine(a.array[0]);
            //Console.WriteLine($"d: {d}");
            //TestSpeed();

            //TestTSS(3);
        }
    }
}