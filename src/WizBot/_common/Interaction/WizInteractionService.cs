namespace WizBot;

public class WizInteractionService : IWizInteractionService, INService
{
    private readonly DiscordSocketClient _client;

    public WizInteractionService(DiscordSocketClient client)
    {
        _client = client;
    }

    public WizInteractionBase Create(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true,
        bool clearAfter = true)
        => new WizButtonInteractionHandler(_client,
            userId,
            button,
            onTrigger,
            onlyAuthor: true,
            singleUse: singleUse,
            clearAfter: clearAfter);

    public WizInteractionBase Create<T>(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, T, Task> onTrigger,
        in T state,
        bool singleUse = true,
        bool clearAfter = true
    )
        => Create(userId,
            button,
            ((Func<T, Func<SocketMessageComponent, Task>>)((data)
                => smc => onTrigger(smc, data)))(state),
            singleUse,
            clearAfter);

    public WizInteractionBase Create(
        ulong userId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true)
        => new WizButtonSelectInteractionHandler(_client,
            userId,
            menu,
            onTrigger,
            onlyAuthor: true,
            singleUse: singleUse);


    /// <summary>
    /// Create an interaction which opens a modal
    /// </summary>
    /// <param name="userId">Id of the author</param>
    /// <param name="button">Button builder for the button that will open the modal</param>
    /// <param name="modal">Modal</param>
    /// <param name="onTrigger">The function that will be called when the modal is submitted</param>
    /// <param name="singleUse">Whether the button is single use</param>
    /// <returns></returns>
    public WizInteractionBase Create(
        ulong userId,
        ButtonBuilder button,
        ModalBuilder modal,
        Func<SocketModal, Task> onTrigger,
        bool singleUse = true)
        => Create(userId,
            button,
            async (smc) =>
            {
                await smc.RespondWithModalAsync(modal.Build());
                var modalHandler = new WizModalSubmitHandler(_client,
                    userId,
                    modal.CustomId,
                    onTrigger,
                    true);
                await modalHandler.RunAsync(smc.Message);
            },
            singleUse: singleUse);
}