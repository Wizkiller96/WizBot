namespace WizBot.Services;

public class GreetGrouper<T>
{
    private readonly Dictionary<ulong, HashSet<T>> _group;
    private readonly object _locker = new();

    public GreetGrouper()
        => _group = new();


    /// <summary>
    ///     Creates a group, if group already exists, adds the specified user
    /// </summary>
    /// <param name="guildId">Id of the server for which to create group for</param>
    /// <param name="toAddIfExists">User to add if group already exists</param>
    /// <returns></returns>
    public bool CreateOrAdd(ulong guildId, T toAddIfExists)
    {
        lock (_locker)
        {
            if (_group.TryGetValue(guildId, out var list))
            {
                list.Add(toAddIfExists);
                return false;
            }

            _group[guildId] = new();
            return true;
        }
    }

    /// <summary>
    ///     Remove the specified amount of items from the group. If all items are removed, group will be removed.
    /// </summary>
    /// <param name="guildId">Id of the group</param>
    /// <param name="count">Maximum number of items to retrieve</param>
    /// <param name="items">Items retrieved</param>
    /// <returns>Whether the group has no more items left and is deleted</returns>
    public bool ClearGroup(ulong guildId, int count, out IReadOnlyCollection<T> items)
    {
        lock (_locker)
        {
            if (_group.TryGetValue(guildId, out var set))
            {
                // if we want more than there are, return everything
                if (count >= set.Count)
                {
                    items = set;
                    _group.Remove(guildId);
                    return true;
                }

                // if there are more in the group than what's needed
                // take the requested number, remove them from the set
                // and return them
                var toReturn = set.TakeWhile(_ => count-- != 0).ToList();
                foreach (var item in toReturn)
                    set.Remove(item);

                items = toReturn;
                // returning falsemeans group is not yet deleted
                // because there are items left
                return false;
            }

            items = Array.Empty<T>();
            return true;
        }
    }
}