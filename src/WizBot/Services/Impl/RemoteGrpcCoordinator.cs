﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grpc.Core;
using WizBot.Common.ModuleBehaviors;
using WizBot.Coordinator;
using WizBot.Services;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Services
{
    public class RemoteGrpcCoordinator : ICoordinator, IReadyExecutor
    {
        private readonly Coordinator.Coordinator.CoordinatorClient _coordClient;
        private readonly DiscordSocketClient _client;

        public RemoteGrpcCoordinator(IBotCredentials creds, DiscordSocketClient client)
        {
            var coordUrl = string.IsNullOrWhiteSpace(creds.CoordinatorUrl)
                ? "http://localhost:3442"
                : creds.CoordinatorUrl;
            
            var channel = Grpc.Net.Client.GrpcChannel.ForAddress(coordUrl);
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

        public void Die(bool graceful)
        {
            _coordClient.Die(new DieRequest()
            {
                Graceful = graceful
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

        public IList<ShardStatus> GetAllShardStatuses()
        {
            var res = _coordClient.GetAllStatuses(new GetAllStatusesRequest());

            return res.Statuses
                .ToArray()
                .Map(s => new ShardStatus()
                {
                    ConnectionState = FromCoordConnState(s.State),
                    GuildCount = s.GuildCount,
                    ShardId = s.ShardId,
                    LastUpdate = s.LastUpdate.ToDateTime(),
                });
        }

        public int GetGuildCount()
        {
            var res = _coordClient.GetAllStatuses(new GetAllStatusesRequest());

            return res.Statuses.Sum(x => x.GuildCount);
        }
        
        public async Task Reload()
        {
            await _coordClient.ReloadAsync(new());
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
                            GuildCount = _client.ConnectionState == ConnectionState.Connected ? _client.Guilds.Count : 0,
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

        private ConnState ToCoordConnState(ConnectionState state)
            => state switch
            {
                ConnectionState.Connecting => ConnState.Connecting,
                ConnectionState.Connected => ConnState.Connected,
                _ => ConnState.Disconnected
            };

        private ConnectionState FromCoordConnState(ConnState state)
            => state switch
            {
                ConnState.Connecting => ConnectionState.Connecting,
                ConnState.Connected => ConnectionState.Connected,
                _ => ConnectionState.Disconnected
            };
    }
}