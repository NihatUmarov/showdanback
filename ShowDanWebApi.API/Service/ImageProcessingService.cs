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

    private async Task<int> GetNextImageIdAsync()
    {
        
        settings.LastImageId += 1000;
        await _context.SaveChangesAsync();

        return Interlocked.Increment(ref _globalImageCounter);
    }

    public async Task<(string full, string thumb)> ProcessAndSaveAvatarAsync(IFormFile file)
    {
       

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
    }

    public async Task<string> ProcessAndSaveMediaAsync(IFormFile file, int mediaType)
    {
        

        return $"{_baseUrl}/{subFolder}/{fileName}";
    }

    public Task DeleteMediaAsync(IEnumerable<string?> urls)
    {
        if (urls == null) return Task.CompletedTask;

        foreach (var url in urls)
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
        int index = result.Length;e
    }
}f

public static class StreamExtensions
{

}
{}