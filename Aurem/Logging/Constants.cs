using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuremCore.FastLogger;

namespace Aurem.Logging
{
    /// <summary>
    /// Provides constants used by the logger, decoder, and other utilities.
    /// </summary>
    internal static class Constants
    {
        /**
         * Shortcuts for event types.
         * Any event that happens multiple times should have a single character representation.
         */
        // Frequent events
        public static readonly string UnitCreated           = "A";
        public static readonly string CreatorNotReady       = "B";
        public static readonly string CreatorProcessingUnit = "C";
        public static readonly string SendingUnitToCreator  = "D";
        public static readonly string AddPreunits           = "E";
        public static readonly string PreunitReady          = "F";
        public static readonly string UnitAdded             = "G";
        public static readonly string ReadyToAdd            = "H";
        public static readonly string DuplicatedUnits       = "I";
        public static readonly string DuplicatedPreunits    = "J";
        public static readonly string UnknownParents        = "K";
        public static readonly string NewTimingUnit         = "L";
        public static readonly string OwnUnitOrdered        = "M";
        public static readonly string LinearOrderExtended   = "N";
        public static readonly string UnitOrdered           = "O";
        public static readonly string SentUnit              = "P";
        public static readonly string PreunitReceived       = "Q";
        public static readonly string SyncStarted           = "R";
        public static readonly string SyncCompleted         = "S";
        public static readonly string GetInfo               = "T";
        public static readonly string SendInfo              = "U";
        public static readonly string GetUnits              = "V";
        public static readonly string SendUnits             = "W";
        public static readonly string PreblockProduced      = "Y";

        // Rare events
        public static readonly string NewEpoch              = "a";
        public static readonly string EpochEnd              = "b";
        public static readonly string SkippingEpoch         = "c";
        public static readonly string ServiceStarted        = "d";
        public static readonly string ServiceStopped        = "e";
        public static readonly string GotWTK                = "f";
        public static readonly string CreatorFinished       = "g";
        public static readonly string ForkDetected          = "h";
        public static readonly string MissingRandomBytes    = "i";
        public static readonly string InvalidControlHash    = "j";
        public static readonly string InvalidEpochProof     = "k";
        public static readonly string InvalidCreator        = "l";
        public static readonly string FreezedParent         = "m";
        public static readonly string FutureLastTiming      = "n";
        public static readonly string UnableToRetrieveEpoch = "o";
        public static readonly string RequestOverload       = "p";

        /// <summary>
        /// Maps short event names to human readable form.
        /// </summary>
        public static Dictionary<string, string> EventTypeDict = new Dictionary<string, string>
        {
            {UnitCreated, "new unit created"},
            {CreatorNotReady, "creator not ready after update"},
            {CreatorProcessingUnit, "creator processing a unit from the belt"},
            {SendingUnitToCreator, "putting a newly added unit on creator's belt"},
            {AddPreunits, "putting preunits in adder started"},
            {PreunitReady, "adding a ready waiting preunit started"},
            {UnitAdded, "unit added to the dag"},
            {ReadyToAdd, "added ready waiting preunits"},
            {DuplicatedUnits, "trying to add units already present in dag"},
            {DuplicatedPreunits, "trying to add preunits already present in adder"},
            {UnknownParents, "trying to add a unit with missing parents"},
            {NewTimingUnit, "new timing unit"},
            {OwnUnitOrdered, "unit created by this process has been ordered"},
            {LinearOrderExtended, "linear order extended"},
            {UnitOrdered, "unit ordered"},
            {SentUnit, "sent a unit through multicast"},
            {PreunitReceived, "multicast has received a preunit"},
            {SyncStarted, "new sync started"},
            {SyncCompleted, "sync completed"},
            {GetInfo, "receiving dag info started"},
            {SendInfo, "sending dag info started"},
            {GetUnits, "receiving preunits started"},
            {SendUnits, "sending units started"},
            {PreblockProduced, "new preblock"},

            {NewEpoch, "new epoch"},
            {EpochEnd, "epoch finished"},
            {SkippingEpoch, "creator skipping epoch without finishing it"},
            {ServiceStarted, "STARTED"},
            {ServiceStopped, "STOPPED"},
            {GotWTK, "received weak threshold key from the setup phase"},
            {CreatorFinished, "creator has finished its work"},
            {ForkDetected, "fork detected in adder"},
            {MissingRandomBytes, "too early to choose the next timing unit, no random bytes for required level"},
            {InvalidControlHash, "invalid control hash"},
            {InvalidEpochProof, "invalid epoch's proof in a unit from a future epoch"},
            {InvalidCreator, "invalid creator of a unit"},
            {FreezedParent, "creator freezed a parent due to some non-compliance"},
            {FutureLastTiming, "creator received timing unit from newer epoch that he's seen"},
            {UnableToRetrieveEpoch, "unable to retrieve an epoch"},
            {RequestOverload, "sync server overloaded with requests"},
        };

        /**
         * Field names.
         */
        public static readonly string Sent = "A";
        public static readonly string Recv = "B";
        public static readonly string Creator = "C";
        public static readonly string ID = "D";
        public static readonly string Epoch = "E";
        public static readonly string ControlHash = "F";
        public static readonly string Height = "H";
        public static readonly string ISID = "I";
        public static readonly string WTKShareProviders = "J";
        public static readonly string WTKThreshold = "K";
        public static readonly string LogLevel = "L";
        public static readonly string Message = "M";
        public static readonly string Size = "N";
        public static readonly string OSID = "O";
        public static readonly string PID = "P";
        public static readonly string Level = "Q";
        public static readonly string Round = "R";
        public static readonly string Service = "S";
        public static readonly string Time = "T";

        /// <summary>
        /// Maps short field names to human readable form.
        /// </summary>
        public static Dictionary<string, string> FieldNameDict = new Dictionary<string, string>
        {
            {Sent, "sent"},
            {Recv, "received"},
            {Creator, "creator"},
            {ID, "ID"},
            {Epoch, "epoch"},
            {ControlHash, "hash"},
            {Height, "height"},
            {ISID, "inSID"},
            {WTKShareProviders, "wtkSP"},
            {WTKThreshold, "wtkThr"},
            {LogLevel, "lvl"},
            {Message, "msg"},
            {Size, "size"},
            {OSID, "outSID"},
            {PID, "PID"},
            {Level, "level"},
            {Round, "round"},
            {Service, "service"},
            {Time, "time"},
        };

        /**
         * Service types.
         */
        public static readonly int CreatorService = 0;
        public static readonly int OrderService = 1;
        public static readonly int AdderService = 2;
        public static readonly int ExtenderService = 3;
        public static readonly int GossipService = 4;
        public static readonly int FetchService = 5;
        public static readonly int MCService = 6;
        public static readonly int RMCService = 7;
        public static readonly int AlertService = 8;
        public static readonly int NetworkService = 9;

        /// <summary>
        /// Maps integer service types to human readable form.
        /// </summary>
        public static Dictionary<int, string> ServiceTypeDict = new Dictionary<int, string>
        {
            {CreatorService, "CREATOR"},
            {OrderService, "ORDERER"},
            {AdderService, "ADDER"},
            {ExtenderService, "EXTENDER"},
            {GossipService, "GOSSIP"},
            {FetchService, "FETCH"},
            {MCService, "MCAST"},
            {RMCService, "RMC"},
            {AlertService, "ALERT"},
            {NetworkService, "NETWORK"},
        };

        /// <summary>
        /// Genesis string.
        /// </summary>
        public static readonly string Genesis = "genesis";
    }
}
