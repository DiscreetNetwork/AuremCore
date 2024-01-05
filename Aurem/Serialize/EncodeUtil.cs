using Aurem.Model;
using AuremCore.Network;
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
            var enc = new Encoder(ms);
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
            var dec = new Decoder(new MemoryStream(bytes));
            return dec.DecodePreunit();
        }

        /// <summary>
        /// Writes an array of DagInfos to a stream. Expects the total array size to be two.
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="s"></param>
        public static void WriteDagInfos(DagInfo[] infos, Stream s)
        {
            var enc = new Encoder(s);
            foreach (var info in infos.Take(2)) enc.EncodeDagInfo(info);
        }

        public static async Task WriteDagInfosAsync(DagInfo[] infos, Stream s)
        {
            var enc = new Encoder(s);
            foreach (var info in infos.Take(2)) await enc.EncodeDagInfoAsync(info);
        }

        public static async Task WriteDagInfosAsync(DagInfo[] infos, Conn conn)
        {
            var enc = new Encoder(conn);
            foreach (var info in infos.Take(2)) await enc.EncodeDagInfoAsync(info);
        }

        /// <summary>
        /// Reads an array of DagInfos from the given stream. Expects the total number in the stream to be two.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DagInfo[] ReadDagInfos(Stream s)
        {
            var infos = new DagInfo[2];
            var dec = new Decoder(s);
            infos[0] = dec.DecodeDagInfo();
            infos[1] = dec.DecodeDagInfo();

            return infos;
        }

        public static (DagInfo[], int) ReadDagInfos(ReadOnlySpan<byte> s)
        {
            var infos = new DagInfo[2];
            (infos[0], var tot) = Decoder.DeserializeDagInfo(s);
            (infos[1], var t) = Decoder.DeserializeDagInfo(s.Slice(tot));

            return (infos, t + tot);
        }

        public static async Task<DagInfo[]> ReadDagInfosAsync(Stream s)
        {
            var infos = new DagInfo[2];
            var dec = new Decoder(s);
            infos[0] = await dec.DecodeDagInfoAsync();
            infos[1] = await dec.DecodeDagInfoAsync();

            return infos;
        }

        public static async Task<DagInfo[]> ReadDagInfosAsync(Conn conn)
        {
            var infos = new DagInfo[2];
            var dec = new Decoder(conn);
            infos[0] = await dec.DecodeDagInfoAsync();
            infos[1] = await dec.DecodeDagInfoAsync();

            return infos;
        }

        /// <summary>
        /// Writes an encoded unit to the stream.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="s"></param>
        public static void WriteUnit(IUnit u, Stream s) => new Encoder(s).EncodeUnit(u);

        public static Task WriteUnitAsync(IUnit u, Stream s) => new Encoder(s).EncodeUnitAsync(u);

        public static Task WriteUnitAsync(IUnit u, Conn conn) => new Encoder(conn).EncodeUnitAsync(u);

        /// <summary>
        /// Writes an encoded preunit to the stream.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="s"></param>
        public static void WritePreunit(IPreunit u, Stream s) => new Encoder(s).EncodeUnit(u);

        public static Task WritePreunitAsync(IPreunit u, Stream s) => new Encoder(s).EncodeUnitAsync(u);

        public static Task WritePreunitAsync(IPreunit u, Conn conn) => new Encoder(conn).EncodeUnitAsync(u);

        /// <summary>
        /// Reads and decodes a preunit from the stream.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IPreunit ReadPreunit(Stream s) => new Decoder(s).DecodePreunit();

        public static Task<IPreunit> ReadPreunitAsync(Stream s) => new Decoder(s).DecodePreunitAsync();

        public static Task<IPreunit> ReadPreunitAsync(Conn conn) => new Decoder(conn).DecodePreunitAsync();

        /// <summary>
        /// Writes a chunk of encoded units to the stream.
        /// </summary>
        /// <param name="units"></param>
        /// <param name="s"></param>
        public static void WriteChunk(IList<IUnit> units, Stream s) => new Encoder(s).EncodeChunk(units);

        public static Task WriteChunkAsync(IList<IUnit> units, Stream s) => new Encoder(s).EncodeChunkAsync(units);

        public static Task WriteChunkAsync(IList<IUnit> units, Conn conn) => new Encoder(conn).EncodeChunkAsync(units);

        /// <summary>
        /// Reads and decodes a chunk of preunit antichains from the reader.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static IPreunit[] ReadChunk(Stream s) => new Decoder(s).DecodeChunk();

        public static Task<IPreunit[]> ReadChunkAsync(Stream s) => new Decoder(s).DecodeChunkAsync();

        public static Task<IPreunit[]> ReadChunkAsync(Conn conn) => new Decoder(conn).DecodeChunkAsync();

        public static int ComputeLayer(IUnit u, Dictionary<IUnit, int> layers)
        {
            var success = layers.TryGetValue(u, out var layer);
            if (!success) layer = 0;

            if (layer == -1)
            {
                var maxParentLayer = 0;
                foreach (var v in u.Parents())
                {
                    if (v == null) continue;
                    var cl = ComputeLayer(v, layers); 
                    if (cl > maxParentLayer)
                    {
                        maxParentLayer = cl;
                    }
                }

                layers[u] = maxParentLayer + 1;
            }

            layers.TryGetValue(u, out layer);
            return layer;
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
