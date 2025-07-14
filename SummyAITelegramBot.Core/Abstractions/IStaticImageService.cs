namespace SummyAITelegramBot.Core.Abstractions;

public interface IStaticImageService
{
    Stream GetImageStream(string fileName, string? dirName = "image");

    void DeleteImage(string fileName, string dirName);
}