using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group("todo")]
    public partial class Todo : NadekoModule<TodoService>
    {
        [Cmd]
        public async Task TodoAdd([Leftover] string todo)
        {
            var result = await _service.AddAsync(ctx.User.Id, todo);
            if (result == TodoAddResult.MaxLimitReached)
            {
                await ReplyErrorLocalizedAsync(strs.todo_add_max_limit);
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoEdit(kwum todoId, [Leftover] string newMessage)
        {
            if (!await _service.EditAsync(ctx.User.Id, todoId, newMessage))
            {
                await ReplyErrorLocalizedAsync(strs.todo_not_found);
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoList()
        {
            var todos = await _service.GetAllTodosAsync(ctx.User.Id);

            if (todos.Length == 0)
            {
                await ReplyErrorLocalizedAsync(strs.todo_list_empty);
                return;
            }

            await ShowTodosAsync(todos);
        }


        [Cmd]
        public async Task TodoComplete(kwum todoId)
        {
            if (!await _service.CompleteTodoAsync(ctx.User.Id, todoId))
            {
                await ReplyErrorLocalizedAsync(strs.todo_not_found);
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoDelete(kwum todoId)
        {
            if (!await _service.DeleteTodoAsync(ctx.User.Id, todoId))
            {
                await ReplyErrorLocalizedAsync(strs.todo_not_found);
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoClear()
        {
            await _service.ClearTodosAsync(ctx.User.Id);

            await ReplyConfirmLocalizedAsync(strs.todo_cleared);
        }


        private async Task ShowTodosAsync(TodoModel[] todos)
        {
            await ctx.SendPaginatedConfirmAsync(0,
                (curPage) =>
                {
                    var eb = _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.todo_list));

                    ShowTodoItem(todos, curPage, eb);

                    return eb;
                },
                todos.Length,
                9);
        }

        private static void ShowTodoItem(IReadOnlyCollection<TodoModel> todos, int curPage, IEmbedBuilder eb)
        {
            foreach (var todo in todos.Skip(curPage * 9).Take(9))
            {
                // green circle and yellow circle emojis
                eb.AddField($"-",
                    $"{(todo.IsDone
                        ? "✅"
                        : "🟡")} {Format.Code(new kwum(todo.Id).ToString())} {todo.Todo}",
                    false);
            }
        }

        [Group("archive")]
        public partial class ArchiveCommands : NadekoModule<TodoService>
        {
            [Cmd]
            public async Task TodoArchiveAdd([Leftover] string name)
            {
                var result = await _service.ArchiveTodosAsync(ctx.User.Id, name);
                if (result == ArchiveTodoResult.NoTodos)
                {
                    await ReplyErrorLocalizedAsync(strs.todo_no_todos);
                    return;
                }

                if (result == ArchiveTodoResult.MaxLimitReached)
                {
                    await ReplyErrorLocalizedAsync(strs.todo_archive_max_limit);
                    return;
                }

                await ctx.OkAsync();
            }

            [Cmd]
            public async Task TodoArchiveList(int page = 1)
            {
                if (--page < 0)
                    return;

                var archivedTodoLists = await _service.GetArchivedTodosAsync(ctx.User.Id);

                if (archivedTodoLists.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.todo_archive_empty);
                    return;
                }

                await ctx.SendPaginatedConfirmAsync(page,
                    (curPage) =>
                    {
                        var eb = _eb.Create()
                            .WithTitle(GetText(strs.todo_archive_list))
                            .WithOkColor();

                        foreach (var archivedList in archivedTodoLists.Skip(curPage * 9).Take(9))
                        {
                            eb.AddField($"id: {archivedList.Id.ToString()}", archivedList.Name, true);
                        }

                        return eb;
                    },
                    archivedTodoLists.Count,
                    9,
                    true);
            }

            [Cmd]
            public async Task TodoArchiveShow(int id)
            {
                var list = await _service.GetArchivedTodoListAsync(ctx.User.Id, id);
                if (list == null || list.Items.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.todo_archive_not_found);
                    return;
                }

                await ctx.SendPaginatedConfirmAsync(0,
                    (curPage) =>
                    {
                        var eb = _eb.Create()
                            .WithOkColor()
                            .WithTitle(GetText(strs.todo_list));

                        ShowTodoItem(list.Items, curPage, eb);

                        return eb;
                    },
                    list.Items.Count,
                    9);
            }

            [Cmd]
            public async Task TodoArchiveDelete(int id)
            {
                if (!await _service.ArchiveDeleteAsync(ctx.User.Id, id))
                {
                    await ctx.ErrorAsync();
                    return;
                }

                await ctx.OkAsync();
            }
        }
    }
}