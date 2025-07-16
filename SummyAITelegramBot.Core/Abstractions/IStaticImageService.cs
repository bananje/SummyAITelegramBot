namespace SummyAITelegramBot.Core.Abstractions;

public interface IStaticImageService
{
    Stream GetImageStream(string fileName, string? dirName = "images");

    void DeleteImage(string fileName, string dirName);
}