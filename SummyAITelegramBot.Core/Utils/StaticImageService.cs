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

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Image not found", fullPath);

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }
}