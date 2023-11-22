using Aurem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Cmd
{
    /// <summary>
    /// Contains utilities for converting the output of an Aurem FastLogger file to a human readable format.
    /// </summary>
    public static class HRLog
    {
        public static void MakeReadableLog(string path, string outpath)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"{path}: file not present");
            }

            var f = File.OpenText(path);
            var o = File.OpenWrite(outpath);

            var dec = new Aurem.Logging.Decoder(o);
            while (!f.EndOfStream)
            {
                var s = f.ReadLine();
                if (s == null) break;

                dec.Write(s);
            }

            f.Close();
            o.Close();
        }
    }
}
