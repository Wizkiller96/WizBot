namespace WizBot.Core.Common
{
    public interface ISeria
    {
        byte[] Serialize<T>(T data);
        T Deserialize<T>(byte[] data);
    }
}