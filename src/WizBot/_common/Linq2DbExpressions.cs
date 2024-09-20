﻿#nullable disable
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Wiz.Common;

public static class Linq2DbExpressions
{
    [ExpressionMethod(nameof(GuildOnShardExpression))]
    public static bool GuildOnShard(ulong guildId, int totalShards, int shardId)
        => throw new NotSupportedException();
    
    private static Expression<Func<ulong, int, int, bool>> GuildOnShardExpression()
        => (guildId, totalShards, shardId)
            => guildId / 4194304 % (ulong)totalShards == (ulong)shardId;
}