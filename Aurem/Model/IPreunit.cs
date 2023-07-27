using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Preunit defines the most general interface for units. It describes unit "in a vacuum", i.e. standalone, without references to parents.
    /// </summary>
    public interface IPreunit
    {
        /// <summary>
        /// Returns the unique id of a set of creators who participated in the making of a dag containing this unit.
        /// </summary>
        /// <returns></returns>
        public uint EpochID();

        /// <summary>
        /// Returns the id of the process that made this unit.
        /// </summary>
        /// <returns></returns>
        public ushort Creator();

        /// <summary>
        /// Returns the signature of this unit.
        /// </summary>
        /// <returns></returns>
        public byte[] Signature();

        /// <summary>
        /// Returns the hash value of this unit.
        /// </summary>
        /// <returns></returns>
        public Hash Hash();

        /// <summary>
        /// Height of a unit is the length of the path between this unit and a dealing unit in the (induced) sub-dag containing all units produced by the same creator.
        /// </summary>
        /// <returns></returns>
        public int Height();

        /// <summary>
        /// View is the crown of the dag below the unit.
        /// </summary>
        /// <returns></returns>
        public Crown View();

        /// <summary>
        /// Returns the encoded bytes of data contained in this unit.
        /// </summary>
        /// <returns></returns>
        public byte[] Data();

        /// <summary>
        /// RandomSourceData is the data contained in the unit needed to maintain the common random source among processes.
        /// </summary>
        /// <returns></returns>
        public byte[] RandomSourceData();

        /// <summary>
        /// Returns a short, human-identifiable name for the unit, for the purpose of quick analysis and identification.
        /// </summary>
        /// <returns></returns>
        public string Nickname() => Hash().Short();

        /// <summary>
        /// Returns a (intended to be) unique number which can be used to identify this unit.
        /// </summary>
        /// <returns></returns>
        public ulong UnitID() => ID(Height(), Creator(), EpochID());

        /// <summary>
        /// Creates a tuple (height, creator, epoch) encoded as a single number.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="creator"></param>
        /// <param name="epoch"></param>
        /// <returns></returns>
        public static ulong ID(int height, ushort creator, uint epoch) => (ulong)height + ((ulong)creator << 16) + ((ulong)epoch << 32);

        /// <summary>
        /// Decodes an ID, i.e. a single number, into a tuple (Height, Creator, Epoch).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static (int Height, ushort Creator, uint Epoch) DecodeID(ulong id)
        {
            int height = (int)(id & 0xFFFF);
            id >>= 16;
            ushort creator = (ushort)(id & 0xFFFF);
            return (height, creator, (uint)(id >> 16));
        }

        /// <summary>
        /// Check whether two units are the same.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is IPreunit u)
            {
                return u.Creator() == Creator() && u.Height() == Height() && u.EpochID() == u.EpochID() && u.Hash().Equals(Hash());
            }
            
            return false;
        }

        /// <summary>
        /// Checks if the unit is a dealing unit.
        /// </summary>
        /// <returns></returns>
        public bool Dealing() => Height() == 0;
    }
}
