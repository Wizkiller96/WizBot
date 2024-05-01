namespace NadekoBot.Extensions;

public partial class ResponseBuilder
{
    public class PaginationSender<T>
    {
        private const string BUTTON_LEFT = "BUTTON_LEFT";
        private const string BUTTON_RIGHT = "BUTTON_RIGHT";

        private static readonly IEmote _arrowLeft = Emote.Parse("<:x:1232256519844790302>");
        private static readonly IEmote _arrowRight = Emote.Parse("<:x:1232256515298295838>");

        private readonly SourcedPaginatedResponseBuilder<T> _paginationBuilder;
        private readonly ResponseBuilder builder;
        private readonly DiscordSocketClient client;
        private int currentPage;

        public PaginationSender(
            SourcedPaginatedResponseBuilder<T> paginationBuilder,
            ResponseBuilder builder
        )
        {
            this._paginationBuilder = paginationBuilder;
            this.builder = builder;

            client = (DiscordSocketClient)builder.ctx.Client;
            currentPage = 0;
        }

        public async Task SendAsync(bool ephemeral = false)
        {
            var lastPage = (_paginationBuilder.TotalElements - 1)
                           / _paginationBuilder.ItemsPerPage;

            var items = (await _paginationBuilder.ItemsFunc(currentPage)).ToArray();
            var embed = await _paginationBuilder.PageFunc(items, currentPage);

            if (_paginationBuilder.AddPaginatedFooter)
                embed.AddPaginatedFooter(currentPage, lastPage);

            SimpleInteraction<T>? maybeInter = null;

            async Task<ComponentBuilder> GetComponentBuilder()
            {
                var cb = new ComponentBuilder();

                cb.WithButton(new ButtonBuilder()
                              .WithStyle(ButtonStyle.Primary)
                              .WithCustomId(BUTTON_LEFT)
                              .WithDisabled(lastPage == 0)
                              .WithEmote(_arrowLeft)
                              .WithDisabled(currentPage <= 0));
                // todo
                // if (interFactory is not null)
                // {
                //     maybeInter = await interFactory(currentPage);
                //
                //     if (maybeInter is not null)
                //         cb.WithButton(maybeInter.Button);
                // }

                cb.WithButton(new ButtonBuilder()
                              .WithStyle(ButtonStyle.Primary)
                              .WithCustomId(BUTTON_RIGHT)
                              .WithDisabled(lastPage == 0 || currentPage >= lastPage)
                              .WithEmote(_arrowRight));

                return cb;
            }

            async Task UpdatePageAsync(SocketMessageComponent smc)
            {
                var pageItems = (await _paginationBuilder.ItemsFunc(currentPage)).ToArray();
                var toSend = await _paginationBuilder.PageFunc(pageItems, currentPage);
                if (_paginationBuilder.AddPaginatedFooter)
                    toSend.AddPaginatedFooter(currentPage, lastPage);

                var component = (await GetComponentBuilder()).Build();

                await smc.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = toSend.Build();
                    x.Components = component;
                });
            }

            var model = builder.Build(ephemeral);

            var component = (await GetComponentBuilder()).Build();
            var msg = await model.TargetChannel
                                 .SendMessageAsync(model.Text,
                                     embed: embed.Build(),
                                     components: component,
                                     messageReference: model.MessageReference);

            async Task OnInteractionAsync(SocketInteraction si)
            {
                try
                {
                    if (si is not SocketMessageComponent smc)
                        return;

                    if (smc.Message.Id != msg.Id)
                        return;

                    await si.DeferAsync();
                    if (smc.User.Id != model.User.Id)
                        return;

                    if (smc.Data.CustomId == BUTTON_LEFT)
                    {
                        if (currentPage == 0)
                            return;

                        --currentPage;
                        _ = UpdatePageAsync(smc);
                    }
                    else if (smc.Data.CustomId == BUTTON_RIGHT)
                    {
                        if (currentPage >= lastPage)
                            return;

                        ++currentPage;
                        _ = UpdatePageAsync(smc);
                    }
                    else if (maybeInter is { } inter && inter.Button.CustomId == smc.Data.CustomId)
                    {
                        await inter.TriggerAsync(smc);
                        _ = UpdatePageAsync(smc);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in pagination: {ErrorMessage}", ex.Message);
                }
            }
            // todo re-add
            // if (lastPage == 0 && interFactory is null)
            //     return;

            if (lastPage == 0)
                return;

            var client = this.client;

            client.InteractionCreated += OnInteractionAsync;

            await Task.Delay(30_000);

            client.InteractionCreated -= OnInteractionAsync;

            await msg.ModifyAsync(mp => mp.Components = new ComponentBuilder().Build());
        }
    }
}