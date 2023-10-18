using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AuremCore.FastLogger;

namespace Aurem.Logging
{
    /// <summary>
    /// Decoder
    /// </summary>
    public class Decoder : Stream
    {
        public Stream Base { get; }

        public override bool CanRead => Base.CanRead;

        public override bool CanSeek => Base.CanSeek;

        public override bool CanWrite => Base.CanWrite;

        public override long Length => Base.Length;

        public override long Position { get => Base.Position; set => Base.Position = value; }

        public Decoder(Stream @base)
        {
            Base = @base;
        }

        public override void Flush()
        {
            Base.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Base.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Base.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Base.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Dictionary<string, object> dataObjs = JsonSerializer.Deserialize<Dictionary<string, object>>(buffer) ?? throw new ArgumentException("expected a JSON string or byte array", nameof(buffer));
            var data = dataObjs.ToDictionary(dataObjs => dataObjs.Key, dataObjs => (JsonElement)dataObjs.Value);

            var res = Decode(data);
            Base.Write(Encoding.ASCII.GetBytes($"{res}\n"));

            //Base.Write(buffer, offset, count);
        }

        public void Write(string json)
        {
            Dictionary<string, object> dataObjs = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? throw new ArgumentException("expected a JSON string or byte array", nameof(json));
            var data = dataObjs.ToDictionary(dataObjs => dataObjs.Key, dataObjs => (JsonElement)dataObjs.Value);

            var res = Decode(data);
            Base.Write(Encoding.ASCII.GetBytes($"{res}\n"));

            //Base.Write(buffer, offset, count);
        }

        private static string Decode(Dictionary<string, JsonElement> data)
        {
            if (data.ContainsKey(Constants.Message))
            {
                var _event = data[Constants.Message];
                if (_event.ValueKind == JsonValueKind.String && _event.GetString() == Constants.Genesis)
                {
                    return $"Beginning of time at {data[Constants.Genesis]}";
                }
            }

            var ret = "";
            if (data.ContainsKey(Constants.Time))
            {
                ret += $"{data[Constants.Time].GetString()}|";
            }

            if (data.ContainsKey(Constants.LogLevel))
            {
                ret += $"{(LogLvl)data[Constants.LogLevel].GetInt32()}|";
            }

            if (data.ContainsKey(Constants.Service))
            {
                ret += $"{Constants.FieldNameDict[Constants.Service]}:{Constants.ServiceTypeDict[data[Constants.Service].GetInt32()]}|";
            }

            var slice = new List<string>();
            foreach ((var k, _) in data)
            {
                if (k == Constants.Time || k == Constants.LogLevel || k == Constants.Message || k == Constants.Service) continue;

                slice.Add(k);
            }

            slice.Sort();
            foreach (var k in slice)
            {
                if (Constants.FieldNameDict.ContainsKey(k))
                {
                    ret += $"{Constants.FieldNameDict[k]} = {data[k]}|";
                }
                else
                {
                    ret += $"{k} = {data[k]}|";
                }
            }

            if (data.ContainsKey(Constants.Message))
            {
                var val = data[Constants.Message].GetString();
                if (Constants.EventTypeDict.ContainsKey(val))
                {
                    val = Constants.EventTypeDict[val];
                }
                ret += $"   {val}";
            }

            return ret;
        }
    }
}
