#nullable disable
namespace WizBot.Modules.Nsfw.Common;

public interface IImageData
{
    ImageData ToCachedImageData(Booru type);
}