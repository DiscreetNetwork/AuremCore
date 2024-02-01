using Aurem.Model;
using Aurem.Syncing.Internals;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using AuremCore.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class Syncer : ISyncer
    {
        private Requests.Gossip gossip;
        private Requests.Fetch fetch;
        private Requests.Multicast mcast;

        private List<IService> servers;
        private List<IService> subservices;
        private Config.Config Conf;

        public Syncer(Config.Config conf, IOrderer orderer, Logger log, bool setup, Network nfetch, Network ngossip, Network nmcast)
        {
            servers = new List<IService>();
            subservices = new List<IService>();
            Exception? err;

            (var serv, fetch) = FetchServer.NewServer(conf, orderer, nfetch, log.With().Val(Logging.Constants.Service, Logging.Constants.FetchService).Logger());
            servers.Add(serv);

            (serv, gossip) = GossipServer.NewServer(conf, orderer, ngossip, log.With().Val(Logging.Constants.Service, Logging.Constants.GossipService).Logger());
            servers.Add(serv);

            if (setup)
            {
                (serv, mcast) = RmcServer.NewServer(conf, orderer, nmcast, log.With().Val(Logging.Constants.Service, Logging.Constants.RMCService).Logger());
                servers.Add(serv);
            }
            else
            {
                (serv, mcast) = MulticastServer.NewServer(conf, orderer, nmcast, log.With().Val(Logging.Constants.Service, Logging.Constants.MCService).Logger());
                servers.Add(serv);
            }

            var networkService = new NetService(nfetch, true);
            subservices.Add(networkService);
            networkService = new NetService(ngossip, true);
            subservices.Add(networkService);
            networkService = new NetService(nmcast, true);
            subservices.Add(networkService);

            Conf = conf;
        }

        public Syncer(Config.Config conf, IOrderer orderer, Logger log, bool setup)
        {
            servers = new List<IService>();

            Server netserv;
            Exception? err;

            (var net, subservices, err) = GetNetwork(conf.Pid, conf.FetchAddresses, new List<IService>(), conf.Timeout, log, conf);
            if (err != null)
            {
                throw err;
            }
            (var serv, fetch) = FetchServer.NewServer(conf, orderer, net, log.With().Val(Logging.Constants.Service, Logging.Constants.FetchService).Logger());
            servers.Add(serv);

            (net, subservices, err) = GetNetwork(conf.Pid, conf.GossipAddresses, subservices, conf.Timeout, log, conf);
            if (err != null)
            {
                throw err;
            }
            (serv, gossip) = GossipServer.NewServer(conf, orderer, net, log.With().Val(Logging.Constants.Service, Logging.Constants.GossipService).Logger());
            servers.Add(serv);

            if (setup)
            {
                (net, subservices, err) = GetNetwork(conf.Pid, conf.RMCAddresses, subservices, conf.Timeout, log, conf);
                if (err != null)
                {
                    throw err;
                }
                (serv, mcast) = RmcServer.NewServer(conf, orderer, net, log.With().Val(Logging.Constants.Service, Logging.Constants.RMCService).Logger());
                servers.Add(serv);
            }
            else
            {
                (net, subservices, err) = GetNetwork(conf.Pid, conf.MCastAddresses, subservices, conf.Timeout, log, conf);
                if (err != null)
                {
                    throw err;
                }
                (serv, mcast) = MulticastServer.NewServer(conf, orderer, net, log.With().Val(Logging.Constants.Service, Logging.Constants.MCService).Logger());
                servers.Add(serv);
            }

            Conf = conf;
        }

        public static (ISyncer, Exception?) New(Config.Config conf, IOrderer orderer, Logger log, bool setup)
        {
            try
            {
                return (new Syncer(conf, orderer, log, setup), null);
            }
            catch (Exception e)
            {
                return (null!, e);
            }
        }

        public static (ISyncer, Exception?) NewSessioned(Config.Config conf, IOrderer orderer, Logger log, Network f, Network g, Network m, bool setup)
        {
            try
            {
                return (new Syncer(conf, orderer, log, setup, f, g, m), null);
            }
            catch (Exception e)
            {
                return (null!, e);
            }
        }

        public Task Multicast(IUnit u) => mcast(u);

        public Task RequestFetch(ushort pid, ulong[] ids) => fetch(pid, ids);

        public Task<bool> RequestGossip(ushort pid) => gossip(pid);

        public async Task Start()
        {
            // TODO: launch starts in parallel
            foreach (var service in subservices)
            {
                await service.Start();
            }
            foreach (var server in servers)
            {
                await server.Start();
            }
        }

        public async Task Stop()
        {
            List<Task> stops = new List<Task>();
            foreach (var service in subservices)
            {
                stops.Add(service.StopAsync());
            }
            foreach (var server in servers)
            {
                stops.Add(server.StopAsync());
            }

            await Task.WhenAll(stops);
        }

        private sealed class NetworkService : IService
        {
            private Server wrapped;

            public NetworkService(Server netserv) 
            {
                wrapped = netserv;
            }

            public Task<Exception?> Start() => Task.FromResult<Exception?>(null);

            public Task StopAsync()
            {
                wrapped.Stop();
                return Task.CompletedTask;
            }
        }

        private sealed class NetService : IService
        {
            private Network wrapped;
            private bool soft;

            public NetService(Network netserv, bool soft = false)
            {
                wrapped = netserv;
                this.soft = soft;
            }

            public Task<Exception?> Start() =>Task.FromResult<Exception?>(null);

            public Task StopAsync()
            {
                if (soft) return wrapped.SoftStopAsync();
                else return wrapped.StopAsync();
            }
        }

        private (Server, List<IService>, Exception?) GetNetserv(string net, ushort pid, List<string> addresses, List<IService> services, TimeSpan timeout, Logger log)
        {
            Exception? err = null;
            switch (net)
            {
                case "udp":
                    throw new NotImplementedException();
                case "pers":
                    throw new NotImplementedException();
                case "loc":
                    var netLoggerLoc = log.With().Val(Logging.Constants.Service, Logging.Constants.NetworkService).Logger();
                    if (LocalServer.Instances == null)
                    {
                        throw new Exception("initialize local server instances before starting Process");
                    }
                    // TODO: implement local
                    throw new NotImplementedException();
                    //var netservLoc = LocalServer.Instances[pid];
                    //var netServiceLoc = new NetworkService(netservLoc);
                    //services.Add(netServiceDefault);
                    //return (netservLoc, services, err);
                default:
                    var netLoggerDefault = log.With().Val(Logging.Constants.Service, Logging.Constants.NetworkService).Logger();
                    var netservDefault = new TCPServer(addresses[pid], addresses.ToArray(), netLoggerDefault, timeout);
                    var netServiceDefault = new NetworkService(netservDefault);
                    services.Add(netServiceDefault);
                    return (netservDefault, services, err);
            }
        }

        private (Network, List<IService>, Exception?) GetNetwork(ushort pid, List<string> addrs, List<IService> services, TimeSpan timeout, Logger log, Config.Config conf)
        {
            Exception? err = null;
            var logger = log.With().Val(Logging.Constants.Service, Logging.Constants.NetworkService).Logger();
            var net = new Network(addrs[pid], addrs.ToArray(), logger, timeout, conf);
            var networkService = new NetService(net);
            services.Add(networkService);
            return (net, services, err);
        }
    }
}
