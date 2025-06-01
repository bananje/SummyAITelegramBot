namespace SummyAITelegramBot.Core.Abstractions;

public interface IStaticImageService
{
    Stream GetImageStream(string fileName);
}