using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class Groth16ProvingKey
    {
        public G1 g1ByAlpha;
        public G1 g1ByBeta;
        public G1 g1ByDelta;

        public G1[] g1ByPowersOfTau;
        public G1[] g1ByLiTauOverDelta; // Li = Li(X), i from l+1 to m
        public G1[] g1ByZTauTimesPTauMinus1;

        public G2 g2ByBeta;
        public G2 g2ByDelta;
        public G2[] g2ByPowersOfTau;

        public QAP Program;
    }
}
