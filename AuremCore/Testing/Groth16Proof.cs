using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class Groth16Proof
    {
        public G1 A;
        public G2 B;
        public G1 C;

        public BigInteger[] PublicInputs;

        public override string ToString()
        {
            return $"A: {A.p}\nB: {B.p}\nC: {C.p}\nPublicInputs: {R1CSLine.BIAtoS(PublicInputs)}";
        }
    }
}
