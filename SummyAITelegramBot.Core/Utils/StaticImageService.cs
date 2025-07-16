using Microsoft.AspNetCore.Hosting;
using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Utils;

public class StaticImageService : IStaticImageService
{
    private readonly string _webRootPath;

    public StaticImageService(IWebHostEnvironment env)
    {
        _webRootPath = env.WebRootPath;
    }

    public Stream GetImageStream(string fileName, string? dirName = "images")
    {
        var fullPath = Path.Combine(_webRootPath, dirName, fileName);
        var defaultPath = Path.Combine(_webRootPath, dirName, "summy_start.jpg");

        if (!File.Exists(fullPath))
        {
            return new FileStream(defaultPath, FileMode.Open, FileAccess.Read);
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public void DeleteImage(string fileName, string dirName)
    {
        if (fileName is null) { return; }

        var fullPath = Path.Combine(_webRootPath, dirName, fileName);

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                // Здесь можно логгировать или пробросить исключение
                Console.WriteLine($"Ошибка при удалении файла: {fullPath} — {ex.Message}");
            }
        }
    }
}