using AuremCore.Core;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class DefaultBlockAuthority : IDataSource
    {
        private CancellationTokenSource _cts;
        private readonly PublisherSocket _finalize = new PublisherSocket();
        private readonly SubscriberSocket _data = new SubscriberSocket();
        private readonly Channel<byte[]> _dataSource = Channel.CreateUnbounded<byte[]>();

        public async Task<string> Finalize(ChannelReader<Preblock> ps)
        {
            await foreach (var pb in ps.ReadAllAsync())
            {
                // near-impossible cases, but check regardless
                if (pb == null || pb.Data == null || pb.Data.Count == 0) continue;
                
                foreach (var data in pb.Data)
                {
                    if (data == null || data.Length == 0) continue;

                    // we must have a block in this unit
                    try
                    {
                        _finalize.SendMoreFrame("final").SendFrame(data);
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync($"DefaultBlockAuthority.Finalize: failed to transmit block data to daemon: {ex.Message}");
                    }

                    break;
                }
            }

            return "";
        }

        public DefaultBlockAuthority()
        {
            _cts = new CancellationTokenSource();
        }

        public async Task Start(int finalizePort, int dataPort)
        {
            try
            {
                _finalize.Bind($"tcp://*:{finalizePort}");
                _data.Connect($"tcp://*:{dataPort}");

                _data.Subscribe("data");

                while (!_cts.IsCancellationRequested)
                {
                    // read from subscriber port
                    var topic = _data.ReceiveFrameBytes();
                    var data = _data.ReceiveFrameBytes();

                    if (topic == null || Encoding.UTF8.GetString(topic) != "data")
                    {
                        await Console.Out.WriteLineAsync($"DefaultBlockAuthority.Start: failed to receive correct data topic from daemon");
                    }

                    if (data == null)
                    {
                        await Console.Out.WriteLineAsync($"DefaultBlockAuthority.Start: failed to receive data from daemon");
                    }

                    await _dataSource.Writer.WriteAsync(data!, _cts.Token);
                }
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync("DBA: [FATAL] - " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void Pause()
        {
            try
            {
                Console.WriteLine("Pausing DBA");
                _finalize.SendMoreFrame("pause").SendFrame(new byte[] { 0 });
                Console.WriteLine("Paused DBA");
            }
            catch (Exception e)
            {
                Console.WriteLine($"DBA: [FATAL] - {e.Message}\n{e.StackTrace}");
            }
        }

        public void Unpause()
        {
            try
            {
                Console.WriteLine("Unpausing DBA");
                _finalize.SendMoreFrame("pause").SendFrame(new byte[] { 1 });
                Console.WriteLine("Unpaused DBA");
            }
            catch (Exception e)
            {
                Console.WriteLine($"DBA: [FATAL] - {e.Message}\n{e.StackTrace}");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _finalize.Close();
            _data.Close();
            _dataSource.Writer.Complete();
        }

        public async Task<byte[]> Get()
        {
            var data = await _dataSource.Reader.ReadAsync();
            return data;
        }
    }
}
