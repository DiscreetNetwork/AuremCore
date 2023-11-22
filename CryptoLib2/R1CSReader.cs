using BN256Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2
{
    internal class R1CSReader
    {
        public class Header
        {
            public uint FieldSize;
            public BigInteger PrimeSize;
            public uint NumWires;
            public uint NumPublicOutputs;
            public uint NumPublicInputs;
            public uint NumPrivateInputs;
            public ulong NumLabels;
            public uint NumConstraints;

            public Header(byte[] data)
            {
                var ms = new MemoryStream(data);
                var br = new BinaryReader(ms);

                FieldSize = br.ReadUInt32();
                Console.WriteLine($"Field Size: {FieldSize}");
                var primeSize = new byte[FieldSize];
                br.Read(primeSize);
                var primeSizeBytes = new byte[FieldSize + 1];
                Array.Copy(primeSize, primeSizeBytes, FieldSize);
                PrimeSize = new BigInteger(primeSizeBytes);
                Console.WriteLine($"Prime Size: {PrimeSize}");
                NumWires = br.ReadUInt32();
                NumPublicOutputs = br.ReadUInt32();
                NumPublicInputs = br.ReadUInt32();
                NumPrivateInputs = br.ReadUInt32();
                NumLabels = br.ReadUInt64();
                NumConstraints = br.ReadUInt32();
                Console.WriteLine($"Wires: {NumWires}\nPubOuts: {NumPublicOutputs}\nPunIns: {NumPublicInputs}\nPrivIns: {NumPrivateInputs}\nLabels: {NumLabels}\nConstraints: {NumConstraints}");
            }
        }

        public class Constraints
        {
            public List<BigInteger[]> A = new();
            public List<BigInteger[]> B = new();
            public List<BigInteger[]> C = new();

            public Constraints(byte[] data, Header header)
            {
                var ms = new MemoryStream(data);
                var br = new BinaryReader(ms);

                for (int i = 0; i < header.NumConstraints; i++)
                {
                    var _cNA = br.ReadUInt32();
                    var _cA = Enumerable.Range(0, (int)header.NumWires).Select(x => BigInteger.Zero).ToArray();
                    for (int c = 0; c < _cNA; c++)
                    {
                        var _cWire = br.ReadUInt32();
                        var bz = new byte[header.FieldSize];
                        br.Read(bz);
                        var cz = new byte[header.FieldSize + 1];
                        Array.Copy(bz, 0, cz, 0, header.FieldSize);
                        _cA[_cWire] = new BigInteger(cz);
                    }
                    A.Add(_cA);

                    var _cNB = br.ReadUInt32();
                    var _cB = Enumerable.Range(0, (int)header.NumWires).Select(x => BigInteger.Zero).ToArray();
                    for (int c = 0; c < _cNB; c++)
                    {
                        var _cWire = br.ReadUInt32();
                        var bz = new byte[header.FieldSize];
                        br.Read(bz);
                        var cz = new byte[header.FieldSize + 1];
                        Array.Copy(bz, 0, cz, 0, header.FieldSize);
                        _cB[_cWire] = new BigInteger(cz);
                    }
                    B.Add(_cB);

                    var _cNC = br.ReadUInt32();
                    var _cC = Enumerable.Range(0, (int)header.NumWires).Select(x => BigInteger.Zero).ToArray();
                    for (int c = 0; c < _cNC; c++)
                    {
                        var _cWire = br.ReadUInt32();
                        var bz = new byte[header.FieldSize];
                        br.Read(bz);
                        var cz = new byte[header.FieldSize + 1];
                        Array.Copy(bz, 0, cz, 0, header.FieldSize);
                        _cC[_cWire] = new BigInteger(cz);
                    }
                    C.Add(_cC);
                    // ...
                }

                R1CS r1cs = new R1CS();
                r1cs.lines = new List<R1CSLine>();
                for (int i = 0; i < header.NumConstraints; i++)
                {
                    var line = new R1CSLine();
                    line.A = A[i];
                    line.B = B[i];
                    line.C = C[i];
                    r1cs.lines.Add(line);
                }

                Console.WriteLine(r1cs.ToStringBits());
            }
        }

        public enum SectionType: uint
        {
            Unknown = 0,
            Header = 1,
            Constraint = 2,
            Wire2LabelID = 3,
            CustomGates = 4,
            CustomGatesApp = 5,
        }

        private static SectionType GetSectionType(uint type)
        {
            return (SectionType)type;
        }
        public R1CSReader() 
        {
            var fsbytes = File.ReadAllBytes("C:\\Users\\brand\\source\\repos\\AuremCore\\AuremCore\\Testing\\out.r1cs");
            var ms = new MemoryStream(fsbytes);
            var br = new BinaryReader(ms);
            if (br.ReadUInt32() != 0x73633172) throw new Exception("magic invalid");
            var ver = br.ReadUInt32();
            var numsec = br.ReadUInt32();

            Console.WriteLine($"version: {ver}, number of sections: {numsec}");

            List<(byte[] data, SectionType type)> sections = new();
            for (int i = 0; i < numsec; i++)
            {
                var sect = br.ReadUInt32();
                var sz = br.ReadUInt64();

                var dat = new byte[sz];
                br.Read(dat);

                Console.WriteLine($"Section Type: {GetSectionType(sect)}, Size: {sz}");
                sections.Add((dat, GetSectionType(sect)));
            }

            // parse header first 
            var headersec = sections.Where(x => x.type == SectionType.Header).FirstOrDefault();
            if (headersec == default) throw new Exception("header not found");

            var header = new Header(headersec.data);

            // now parse constraints
            var conssec = sections.Where(x => x.type == SectionType.Constraint).FirstOrDefault();
            if (conssec == default) throw new Exception("constraints not found");

            var cons = new Constraints(conssec.data, header);
            Console.WriteLine("break here");
        }
    }
}
