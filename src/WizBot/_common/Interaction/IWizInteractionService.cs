namespace WizBot;

public interface IWizInteractionService
{
    public WizInteractionBase Create(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true,
        bool clearAfter = true);

    public WizInteractionBase Create<T>(
        ulong userId,
        ButtonBuilder button,
        Func<SocketMessageComponent, T, Task> onTrigger,
        in T state,
        bool singleUse = true,
        bool clearAfter = true);

    WizInteractionBase Create(
        ulong userId,
        SelectMenuBuilder menu,
        Func<SocketMessageComponent, Task> onTrigger,
        bool singleUse = true);
    
    WizInteractionBase Create(
        ulong userId, 
        ButtonBuilder button,
        ModalBuilder modal,
        Func<SocketModal, Task> onTrigger,
        bool singleUse = true);
    
}