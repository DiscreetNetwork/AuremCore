using BN256Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2
{
    internal class Groth16VerificationKey
    {
        public G1 g1;
        public G2 g2;

        public G1[] g1ByLiTauOverGamma; // i is 0 to l

        public G2 g2ByGamma;
        public G2 g2ByDelta;

        public GT gtByAlphaTimesBeta;

        public QAP Program;
    }
}
