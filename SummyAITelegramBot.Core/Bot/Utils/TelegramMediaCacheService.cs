using Microsoft.AspNetCore.Hosting;
using SummyAITelegramBot.Core.Bot.Abstractions;
using TL;
using WTelegram;

namespace SummyAITelegramBot.Core.Bot.Utils;

public class TelegramMediaCacheService : IMediaCacheService
{
    private readonly Client _client;
    private readonly string _cacheDir;

    public TelegramMediaCacheService(Client client, IWebHostEnvironment env)
    {
        _client = client;
        _cacheDir = Path.Combine(env.WebRootPath, "media_cache");

        if (!Directory.Exists(_cacheDir))
            Directory.CreateDirectory(_cacheDir);
    }

    public async Task<string?> SaveMediaAsync(Message message)
    {
        if (message.media is MessageMediaPhoto photoMedia)
        {
            var photo = photoMedia.photo as Photo;
            if (photo == null) return null;

            var size = photo.sizes.OrderByDescending(s => s.Type).FirstOrDefault();
            if (size == null) return null;

            var fileName = $"photo_{message.from_id}_{photo.id}.jpg";
            var filePath = Path.Combine(_cacheDir, fileName);

            // Если файл уже существует — удалим его, чтобы избежать конфликта
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); }
                catch (IOException) { return null; } // не удалось удалить, файл используется — лучше выйти
            }

            var location = new InputPhotoFileLocation
            {
                id = photo.id,
                access_hash = photo.access_hash,
                file_reference = photo.file_reference,
                thumb_size = size.Type
            };

            await using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await _client.DownloadFileAsync(location, fs);
            }

            return filePath;
        }

        if (message.media is MessageMediaDocument docMedia && docMedia.document is Document doc)
        {
            var mime = doc.mime_type ?? "application/octet-stream";
            var extension = GetExtensionFromMimeType(mime);
            var fileName = $"doc_{doc.id}.{extension}";
            var filePath = Path.Combine(_cacheDir, fileName);

            // Если файл уже существует — удалим
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); }
                catch (IOException) { return null; }
            }

            var location = new InputDocumentFileLocation
            {
                id = doc.id,
                access_hash = doc.access_hash,
                file_reference = doc.file_reference
            };

            await using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await _client.DownloadFileAsync(location, fs);
            }

            return filePath;
        }

        return null;
    }

    private string GetExtensionFromMimeType(string mime)
    {
        return mime switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "video/mp4" => "mp4",
            "application/pdf" => "pdf",
            _ => "bin"
        };
    }
}