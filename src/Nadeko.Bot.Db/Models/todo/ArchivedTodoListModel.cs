namespace NadekoBot.Db.Models;

public sealed class ArchivedTodoListModel
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public string Name { get; set; }
    public List<TodoModel> Items { get; set; }
}