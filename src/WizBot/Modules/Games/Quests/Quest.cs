namespace WizBot.Modules.Games.Quests;

public record class Quest(
    QuestIds Id,
    string Name,
    string Description,
    QuestEventType TriggerEvent,
    int RequiredAmount
);