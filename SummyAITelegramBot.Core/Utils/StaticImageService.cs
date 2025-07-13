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

    public Stream GetImageStream(string fileName)
    {
        var fullPath = Path.Combine(_webRootPath, "images", fileName);
        var defaultPath = Path.Combine(_webRootPath, "images", "summy_start.png");

        if (!File.Exists(fullPath))
        {
            return new FileStream(defaultPath, FileMode.Open, FileAccess.Read);
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }
}