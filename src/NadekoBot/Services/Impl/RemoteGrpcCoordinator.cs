using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grpc.Core;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Coordinator;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using Serilog;

namespace NadekoBot.Services
{
    public class RemoteGrpcCoordinator : ICoordinator, IReadyExecutor
    {
        private readonly Coordinator.Coordinator.CoordinatorClient _coordClient;
        private readonly DiscordSocketClient _client;

        public RemoteGrpcCoordinator(IBotCredentials creds, DiscordSocketClient client)
        {
            // todo should use credentials
            var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:3443");
            _coordClient = new(channel);
            _client = client;
        }
        
        public bool RestartBot()
        {
            _coordClient.RestartAllShards(new RestartAllRequest
            { 

            });

            return true;
        }

        public void Die()
        {
            _coordClient.Die(new DieRequest()
            {
                Graceful = false
            });
        }

        public bool RestartShard(int shardId)
        {
            _coordClient.RestartShard(new RestartShardRequest
            {
                ShardId = shardId,
            });

            return true;
        }

        public IEnumerable<ShardStatus> GetAllShardStatuses()
        {
            var res = _coordClient.GetAllStatuses(new GetAllStatusesRequest());

            return res.Statuses
                .ToArray()
                .Map(s => new ShardStatus()
                {
                    ConnectionState = FromCoordConnState(s.State),
                    Guilds = s.GuildCount,
                    ShardId = s.ShardId,
                    Time = s.LastUpdate.ToDateTime(),
                });
        }

        public int GetGuildCount()
        {
            var res = _coordClient.GetAllStatuses(new GetAllStatusesRequest());

            return res.Statuses.Sum(x => x.GuildCount);
        }

        public Task OnReadyAsync()
        {
            Task.Run(async () =>
            {
                var gracefulImminent = false;
                while (true)
                {
                    try
                    {
                        var reply = await _coordClient.HeartbeatAsync(new HeartbeatRequest
                        {
                            State = ToCoordConnState(_client.ConnectionState),
                            GuildCount = _client.ConnectionState == Discord.ConnectionState.Connected ? _client.Guilds.Count : 0,
                            ShardId = _client.ShardId,
                        }, deadline: DateTime.UtcNow + TimeSpan.FromSeconds(10));
                        gracefulImminent = reply.GracefulImminent;
                    }
                    catch (RpcException ex)
                    {
                        if (!gracefulImminent)
                        {
                            Log.Warning(ex, "Hearbeat failed and graceful shutdown was not expected: {Message}",
                                ex.Message);
                            break;
                        }

                        await Task.Delay(22500).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected heartbeat exception: {Message}", ex.Message);
                        break;
                    }

                    await Task.Delay(7500).ConfigureAwait(false);
                }

                Environment.Exit(5);
            });

            return Task.CompletedTask;
        }

        private ConnState ToCoordConnState(Discord.ConnectionState state)
            => state switch
            {
                Discord.ConnectionState.Connecting => ConnState.Connecting,
                Discord.ConnectionState.Connected => ConnState.Connected,
                _ => ConnState.Disconnected
            };

        private Discord.ConnectionState FromCoordConnState(ConnState state)
            => state switch
            {
                ConnState.Connecting => Discord.ConnectionState.Connecting,
                ConnState.Connected => Discord.ConnectionState.Connected,
                _ => Discord.ConnectionState.Disconnected
            };
    }
}