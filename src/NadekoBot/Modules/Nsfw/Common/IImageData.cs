namespace NadekoBot.Modules.Nsfw.Common
{
    public interface IImageData
    {
        ImageData ToCachedImageData(Booru type);
    }
}