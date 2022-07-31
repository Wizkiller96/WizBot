﻿using System.Threading.Channels;
using Serilog;

namespace Wiz.Common;

public sealed class QueueRunner
{
    private readonly Channel<Func<Task>> _channel;
    private readonly int _delayMs;

    public QueueRunner(int delayMs = 0, int maxCapacity = -1)
    {
        if (delayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(delayMs));

        _delayMs = delayMs;
        _channel = maxCapacity switch
        {
            0 or < -1 => throw new ArgumentOutOfRangeException(nameof(maxCapacity)),
            -1 => Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = true,
            }),
            _ => Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(maxCapacity)
            {
                Capacity = maxCapacity,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = true
            })
        };
    }

    public async Task RunAsync(CancellationToken cancel = default)
    {
        while (true)
        {
            var func = await _channel.Reader.ReadAsync(cancel);
            
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception executing a staggered func: {ErrorMessage}", ex.Message);
            }
            finally
            {
                if (_delayMs != 0)
                {
                    await Task.Delay(_delayMs, cancel);
                }
            }
        }
    }
    
    public ValueTask EnqueueAsync(Func<Task> action)
        => _channel.Writer.WriteAsync(action);
}