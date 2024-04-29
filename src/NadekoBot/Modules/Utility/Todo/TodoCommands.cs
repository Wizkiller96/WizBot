using NadekoBot.Db.Models;
using System.Text;

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
                await Response().Error(strs.todo_add_max_limit).SendAsync();
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoEdit(kwum todoId, [Leftover] string newMessage)
        {
            if (!await _service.EditAsync(ctx.User.Id, todoId, newMessage))
            {
                await Response().Error(strs.todo_not_found).SendAsync();
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
                await Response().Error(strs.todo_list_empty).SendAsync();
                return;
            }

            await ShowTodosAsync(todos);
        }


        [Cmd]
        public async Task TodoComplete(kwum todoId)
        {
            if (!await _service.CompleteTodoAsync(ctx.User.Id, todoId))
            {
                await Response().Error(strs.todo_not_found).SendAsync();
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoDelete(kwum todoId)
        {
            if (!await _service.DeleteTodoAsync(ctx.User.Id, todoId))
            {
                await Response().Error(strs.todo_not_found).SendAsync();
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        public async Task TodoClear()
        {
            await _service.ClearTodosAsync(ctx.User.Id);

            await Response().Confirm(strs.todo_cleared).SendAsync();
        }


        private async Task ShowTodosAsync(TodoModel[] todos)
        {
            await ctx.SendPaginatedConfirmAsync(0,
                (curPage) =>
                {
                    var eb = new EmbedBuilder()
                             .WithOkColor()
                             .WithTitle(GetText(strs.todo_list));

                    ShowTodoItem(todos, curPage, eb);

                    return eb;
                },
                todos.Length,
                9);
        }

        private static void ShowTodoItem(IReadOnlyCollection<TodoModel> todos, int curPage, EmbedBuilder eb)
        {
            var sb = new StringBuilder();
            foreach (var todo in todos.Skip(curPage * 9).Take(9))
            {
                sb.AppendLine($"{(todo.IsDone ? "✔" : "□")} {Format.Code(new kwum(todo.Id).ToString())} {todo.Todo}");

                sb.AppendLine("---");
            }

            eb.WithDescription(sb.ToString());
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
                    await Response().Error(strs.todo_no_todos).SendAsync();
                    return;
                }

                if (result == ArchiveTodoResult.MaxLimitReached)
                {
                    await Response().Error(strs.todo_archive_max_limit).SendAsync();
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
                    await Response().Error(strs.todo_archive_empty).SendAsync();
                    return;
                }

                await ctx.SendPaginatedConfirmAsync(page,
                    (curPage) =>
                    {
                        var eb = new EmbedBuilder()
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
                    await Response().Error(strs.todo_archive_not_found).SendAsync();
                    return;
                }

                await ctx.SendPaginatedConfirmAsync(0,
                    (curPage) =>
                    {
                        var eb = new EmbedBuilder()
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