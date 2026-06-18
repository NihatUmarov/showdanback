using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ShowDanWebApi.API.Service;

public interface IImageProcessingService
{
    Task<(string full, string thumb)> ProcessAndSaveAvatarAsync(IFormFile file);
    Task<string> ProcessAndSaveMediaAsync(IFormFile file, int mediaType);
    Task DeleteMediaAsync(IEnumerable<string?> urls);
}

public class ImageProcessingService : IImageProcessingService
{
    private readonly AppDbContext _context;
    private readonly string _baseUploadPath;
    private readonly string _baseUrl;
    private static int _globalImageCounter = 0;
    private static readonly object _initLock = new();

    private static readonly WebpEncoder _optimizedWebpEncoder = new()
    {
        Quality = 75,
        Method = WebpEncodingMethod.Level6
    };

    private static readonly WebpEncoder _thumbWebpEncoder = new()
    {
        Method = WebpEncodingMethod.Level5
    };

    public ImageProcessingService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _baseUploadPath = config["Storage:RootPath"] ?? "/var/www/uploads";
        _baseUrl = config["Storage:BaseUrl"]?.TrimEnd('/') ?? "https://studio.ktlgo.ru:1433/f";
    }

    private async Task<int> GetNextImageIdAsync()
    {
        if (_globalImageCounter != 0) return Interlocked.Increment(ref _globalImageCounter);

        var settings = await _context.Settings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new Core.Entities.Settings.Settings { LastImageId = 1000 };
            _context.Settings.Add(settings);
            await _context.SaveChangesAsync();
        }

        lock (_initLock)
        {
            if (_globalImageCounter == 0)
            {
                _globalImageCounter = settings.LastImageId;
            }
        }
        settings.LastImageId += 1000;
        await _context.SaveChangesAsync();

        return Interlocked.Increment(ref _globalImageCounter);
    }

    public async Task<(string full, string thumb)> ProcessAndSaveAvatarAsync(IFormFile file)
    {
        int nextId = await GetNextImageIdAsync();
        string baseName = $"{Base62Converter.Encode(nextId)}{Random.Shared.Next(100, 999)}";
        string targetPath = Path.Combine(_baseUploadPath, "p");

        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Metadata.ExifProfile = null;

        string fullFileName = $"{baseName}_f.webp";
        string thumbFileName = $"{baseName}_t.webp";
        int origWidth = image.Width;
        int origHeight = image.Height;

        var fullImgTask = Task.Run(async () => {
            using var fullImg = image.Clone(ctx => {
                int targetSize = Math.Min(Math.Min(origWidth, origHeight), 1080);

                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(targetSize, targetSize),
                    Mode = ResizeMode.Crop,
                    Sampler = KnownResamplers.MitchellNetravali
                });
            });
            await fullImg.SaveAsync(Path.Combine(targetPath, fullFileName), _optimizedWebpEncoder);
        });

        var thumbImgTask = Task.Run(async () => {
            using var thumbImg = image.Clone(ctx => {
                int targetSize = Math.Min(Math.Min(origWidth, origHeight), 200);

                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(targetSize, targetSize),
                    Mode = ResizeMode.Crop,
                    Sampler = KnownResamplers.MitchellNetravali
                });
            });
            await thumbImg.SaveAsync(Path.Combine(targetPath, thumbFileName), _thumbWebpEncoder);
        });

        await Task.WhenAll(fullImgTask, thumbImgTask);

        return ($"{_baseUrl}/p/{fullFileName}", $"{_baseUrl}/p/{thumbFileName}");
    }

    public async Task<string> ProcessAndSaveMediaAsync(IFormFile file, int mediaType)
    {
        int nextId = await GetNextImageIdAsync();
        string extension = mediaType == 3 ? Path.GetExtension(file.FileName) : ".webp";
        string fileName = $"{Base62Converter.Encode(nextId)}{Random.Shared.Next(100, 999)}{extension}";
        string subFolder = mediaType switch { 2 => "w", 3 => "a", 4 => "n", _ => "o" };
        string targetPath = Path.Combine(_baseUploadPath, subFolder);

        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
        string fullPath = Path.Combine(targetPath, fileName);

        if (mediaType == 3)
        {
            await System.IO.File.WriteAllBytesAsync(fullPath, await file.OpenReadStream().ToByteArrayAsync());
        }
        else
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());
            image.Metadata.ExifProfile = null;

            if (image.Width > 1080 || image.Height > 1080)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(1080, 1080),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.MitchellNetravali
                }));
            }

            await image.SaveAsync(fullPath, _optimizedWebpEncoder);
        }

        return $"{_baseUrl}/{subFolder}/{fileName}";
    }

    public Task DeleteMediaAsync(IEnumerable<string?> urls)
    {
        if (urls == null) return Task.CompletedTask;

        foreach (var url in urls)
        {
            if (string.IsNullOrEmpty(url)) continue;

            string relativePath = url.Replace($"{_baseUrl}/", "");
            string fullPath = Path.Combine(_baseUploadPath, relativePath);

            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception)
            {
            }
        }
        return Task.CompletedTask;
    }
}


public static class Base62Converter
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static string Encode(int number)
    {
        if (number == 0) return "0";
        Span<char> result = stackalloc char[12];
        int index = result.Length;

        while (number > 0)
        {
            result[--index] = Alphabet[number % 62];
            number /= 62;
        }

        return new string(result[index..]);
    }
}

public static class StreamExtensions
{
    public static async Task<byte[]> ToByteArrayAsync(this Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}