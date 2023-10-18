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
using AuremCore.Core;
using BN256Core.Extensions;

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

        public static void TestWeakThreshold(int n, int tot)
        {
            var t = TUtil.MinimalTrusted((ushort)n);

            var wtcs = new WeakThresholdKey[n];
            var tcs1 = new ThresholdKey[n];
            var tcs2 = new ThresholdKey[n];

            var shareProviders = new Dictionary<ushort, bool>();

            for (ushort i = 0; i < n; i++)
            {
                shareProviders[i] = true;
            }

            var skeys = new P2PSecretKey[n];
            var pkeys = new P2PPublicKey[n];
            var p2pkeys = new SymmetricKey[n][];

            for (int i = 0; i < n; i++)
            {
                (pkeys[i], skeys[i]) = P2P.GenerateKeys();
            }

            for (int i = 0; i < n; i++)
            {
                p2pkeys[i] = P2P.Keys(skeys[i], pkeys, (ushort)i);
            }

            for (int q = 0; q < tot; q++)
            {
                var gtc1 = TSS.CreateRandom((ushort)n, t, out _);
                var tc1 = gtc1.Encrypt(p2pkeys[0]);
                var gtc2 = TSS.CreateRandom((ushort)n, t, out _);
                var tc2 = gtc2.Encrypt(p2pkeys[1]);

                var tc1Enc = tc1.Encode();
                var tc2Enc = tc2.Encode();

                for (ushort i = 0; i < n; i++)
                {
                    (tcs1[i], _) = ThresholdKey.Decode(tc1Enc, 0, i, p2pkeys[i][0]);
                    (tcs2[i], _) = ThresholdKey.Decode(tc2Enc, 1, i, p2pkeys[i][1]);
                    wtcs[i] = WeakThresholdKey.Create(new ThresholdKey[] { tcs1[i], tcs2[i] }, shareProviders);
                }

                var msg = Encoding.ASCII.GetBytes("xyz");

                var shares = new Share[n];
                for (ushort i = 0; i < n; i++)
                {
                    shares[i] = wtcs[i].CreateShare(msg)!;
                }

                // check some shit
                var sh1 = tcs1[0].CreateShare(msg);
                var sh2 = tcs2[0].CreateShare(msg);

                var shs = Share.SumShares(new Share[] { sh1!, sh2! });
                var shsEnc = shs.Marshal();
                var sh0Enc = shares[0].Marshal();
                if (shsEnc.BEquals(sh0Enc))
                {
                    Console.WriteLine("Verified WTK share to equal sum of single coin shares (TK shares)");
                }
                else
                {
                    Console.WriteLine("Failed to verify WTK share as equal to single coin shares");
                }

                for (ushort i = 0; i < n; i++)
                {
                    (var sig, _) = wtcs[i].CombineShares(shares);
                    if (wtcs[i].VerifySignature(sig!, msg))
                    {
                        Console.WriteLine($"Verified signature for round={q}, pid={i}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to verify signature for round={q}, pid={i}");
                    }
                }
            }
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
                    shares[i] = tcs[i].CreateShare(msg)!;
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

        public static async Task DoSomeSleeping(int x, WaitGroup wg)
        {
            wg.Add(1);
            await Task.Delay(x * 1000);
            await Console.Out.WriteLineAsync($"Task {x} completed.");
            wg.Done();
        }

        public static async Task Main(string[] args)
        {
            (var v, var s) = VerificationKey.GenerateKeys();
            var vE = new VerificationKey();
            var v2 = VerificationKey.Add(v, vE);

            await Console.Out.WriteLineAsync($"V1: {PrintUtil.Hexify(v.Marshal())}");
            await Console.Out.WriteLineAsync($"V2: {PrintUtil.Hexify(v2.Marshal())}");

            if (v.Marshal().BEquals(v2.Marshal()))
            {
                await Console.Out.WriteLineAsync("we did it");
            }
            else
            {
                await Console.Out.WriteLineAsync("problem here");
            }

            TestWeakThreshold(4, 1);

            //TestRSA();
            //var npc = 4;
            //var symmkeys = new SymmetricKey[npc][];
            //var p2pskeys = new P2PSecretKey[npc];
            //var p2ppkeys = new P2PPublicKey[npc];
            //for (int i =  0; i < npc; i++)
            //{
            //    (p2ppkeys[i], p2pskeys[i]) = P2P.GenerateKeys();
            //}

            //for (int i = 0; i < npc; i++)
            //{
            //    symmkeys[i] = P2P.Keys(p2pskeys[i], p2ppkeys, (ushort)i);
            //}


            //var tss = new TSS[npc];
            //var pvs = new PolyVerifier[npc];
            //var tcs = new ThresholdKey[npc];
            //var tcse = new byte[npc][];
            //for (int i = 0; i < npc; i++)
            //{
            //    pvs[i] = PolyVerifier.New(npc, TUtil.MinimalTrusted((ushort)npc));
            //    tss[i] = TSS.CreateSeeded((ushort)npc, TUtil.MinimalTrusted((ushort)npc), 13 + i, out _);
            //    tcs[i] = tss[i].Encrypt(symmkeys[i]);
            //    tcse[i] = tcs[i].Encode();
            //}

            //var tcsd = new ThresholdKey[npc][];
            //for (int i = 0; i < npc; i++)
            //{
            //    tcsd[i] = new ThresholdKey[npc];
            //    for (int j = 0; j < npc; j++)
            //    {
            //        (tcsd[i][j], var ok) = ThresholdKey.Decode(tcse[i], (ushort)j, (ushort)i, symmkeys[i][j]);
            //        var pver = tcsd[i][j].PolyVerify(pvs[i]);
            //    }
            //}

            //var numtss = 100;
            //var nproc = 100;
            //Stopwatch sw = Stopwatch.StartNew();
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine(TUtil.Lagrange(Enumerable.Range(0, 10).Select(x => (long)x).ToArray(), (long)i));
            
            //}
            //TestThreshold(nproc, numtss);
            //for (int i = 0; i < numtss; i++) TestThreshold(nproc);

            //BN a = new BN();
            //BigInteger c = new BigInteger();

           
            //c = new BigInteger(252);
            //BN.ToBN(c, a.array);
            //var d = BN.FromBN(a);
            //Console.WriteLine(a.array[0]);
            //Console.WriteLine($"d: {d}");
            //TestSpeed();

            //TestTSS(3);

            //WaitGroup wg = new WaitGroup();

            //var tasks = Enumerable.Range(1, 10).Select(x => DoSomeSleeping(x, wg));

            //foreach (var task in tasks) _ = Task.Run(async () => await task);

            //wg.Add(1);
            //try
            //{
            //    await Task.Delay(100);
            //}
            //finally
            //{
            //    wg.Done();
            //}

            //await wg.WaitAsync();
            //Console.WriteLine("complete");
        }
    }
}