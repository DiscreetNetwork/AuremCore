using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Serialize
{
    public static class EncodeUtil
    {
        /// <summary>
        /// Encodes a unit into a byte array.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static byte[] EncodeUnit(IPreunit unit)
        {
            using var ms = new MemoryStream();
            using var enc = new Encoder(ms);
            enc.EncodeUnit(unit);
            return ms.ToArray();
        }

        /// <summary>
        /// Decodes the given data into a preunit.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static IPreunit DecodeUnit(byte[] bytes)
        {
            using var dec = new Decoder(new MemoryStream(bytes));
            return dec.DecodePreunit();
        }

        /// <summary>
        /// Writes an array of DagInfos to a stream. Expects the total array size to be two.
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="s"></param>
        public static void WriteDagInfos(DagInfo[] infos, Stream s)
        {
            using var enc = new Encoder(s);
            foreach (var info in infos.Take(2)) enc.EncodeDagInfo(info);
        }

        /// <summary>
        /// Reads an array of DagInfos from the given stream. Expects the total number in the stream to be two.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DagInfo[] ReadDagInfos(Stream s)
        {
            var infos = new DagInfo[2];
            using var dec = new Decoder(s);
            infos[0] = dec.DecodeDagInfo();
            infos[1] = dec.DecodeDagInfo();

            return infos;
        }

        /// <summary>
        /// Writes an encoded unit to the stream.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="s"></param>
        public static void WriteUnit(IUnit u, Stream s) => new Encoder(s).EncodeUnit(u);

        /// <summary>
        /// Writes an encoded preunit to the stream.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="s"></param>
        public static void WritePreunit(IPreunit u, Stream s) => new Encoder(s).EncodeUnit(u);

        /// <summary>
        /// Reads and decodes a preunit from the stream.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IPreunit ReadPreunit(Stream s) => new Decoder(s).DecodePreunit();

        /// <summary>
        /// Writes a chunk of encoded units to the stream.
        /// </summary>
        /// <param name="units"></param>
        /// <param name="s"></param>
        public static void WriteChunk(IList<IUnit> units, Stream s) => new Encoder(s).EncodeChunk(units);

        /// <summary>
        /// Reads and decodes a chunk of preunit antichains from the reader.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IPreunit[] ReadChunk(Stream s) => new Decoder(s).DecodeChunk();

        public static int ComputeLayer(IUnit u, Dictionary<IUnit, int> layers)
        {
            if (layers[u] == -1)
            {
                var maxParentLayer = 0;
                foreach (var v in u.Parents())
                {
                    var cl = ComputeLayer(v, layers); 
                    if (cl > maxParentLayer)
                    {
                        maxParentLayer = cl;
                    }
                }
            }

            return layers[u];
        }

        /// <summary>
        /// Sorts the given ordered collection of units according to the following convention:
        /// <list type="bullet">
        /// <item>Units are in ascending order with respect to their EpochID</item>
        /// <item>Within each epoch, units are sorted topologically (first parents, then children)</item>
        /// </list>
        /// </summary>
        /// <param name="units"></param>
        /// <returns></returns>
        public static List<IUnit> SortChunk(IList<IUnit> units) // TODO: O(n^3) is pretty bad, optimize later
        {
            if (units == null || units.Count == 0) return units?.ToList() ?? new List<IUnit>();
            
            var layers = new Dictionary<IUnit, int>();
            foreach (var u in units)
            {
                layers[u] = -1;
            }

            foreach (var u in units)
            {
                layers[u] = ComputeLayer(u, layers);
            }

            var maxLayer = -1;
            var minEpoch = units[0].EpochID();
            var maxEpoch = units[0].EpochID();
            foreach (var u in units)
            {
                if (layers[u] > maxLayer) maxLayer = layers[u];
                if (u.EpochID() > maxEpoch) maxEpoch = u.EpochID();
                if (u.EpochID() < minEpoch) minEpoch = u.EpochID();
            }

            var result = new List<IUnit>(units.Count);
            for (uint epoch = minEpoch; epoch <= maxEpoch; epoch++)
            {
                for (int layer = 0; layer <= maxLayer; layer++)
                {
                    foreach (var u in units)
                    {
                        if (u.EpochID() == epoch && layers[u] == layer)
                        {
                            result.Add(u);
                        }
                    }
                }
            }

            return result;
        }
    }
}
