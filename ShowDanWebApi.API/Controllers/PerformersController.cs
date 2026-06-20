using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Core.Language;
using ShowDanWebApi.Data;
using System.Text.Json;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/performers")]
public class PerformersController : BaseController
{
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            Birthday = user.Birthday,
            GenderCode = user.GenderCode,
            AvatarUrl = user.PhotoUrl,
            CurrencyCode = user.CurrencyCode ?? "USD"
        };
        dto.Description = service.Description?.Get(CurrentLang) ?? "";
        dto.WorkStyle = service.WorkStyle; 

        return Ok(dto);
    
            }


        AddTitlesToTranslate(texts, payload.NewPersonalPhotoTitles, "p_photo");
        AddTitlesToTranslate(texts, payload.NewLivePhotoTitles, "l_photo");
        AddTitlesToTranslate(texts, payload.NewAudioTitles, "audio");
x
            filesToDelete.AddRange(service.AudiosPersonal!.Where(a => payload.DeleteAudioUrls.Contains(a.Url)).Select(a => a.Url));
            service.AudiosPersonal!.RemoveAll(a => payload.DeleteAudioUrls.Contains(a.Url));
        }

        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }

        if (hasNewAvatar && !string.IsNullOrEmpty(user.PhotoUrl))
        {
            filesToDelete.Add(user.PhotoUrl);
            filesToDelete.Add(user.PhotoThumbUrl!);
        }

        if (filesToDelete.Count > 0) await _imageService.DeleteMediaAsync(filesToDelete);
    }

    private async Task ProcessAndAppendUploadsAsync(List<IFormFile>? files, List<string>? titles, string prefix, Dictionary<string, MultiLang> translations, List<MediaItem> targetList, int mediaType)
    {
        if (files == null || files.Count == 0) return;

        for (int i = 0; i < files.Count; i++)
        {
            var url = await _imageService.ProcessAndSaveMediaAsync(files[i], mediaType);
            var rawTitle = titles?.ElementAtOrDefault(i) ?? Path.GetFileNameWithoutExtension(files[i].FileName);
            if (string.IsNullOrWhiteSpace(rawTitle)) rawTitle = "Media";

            targetList.Add(new MediaItem { Url = url, Title = rawTitle });
        }
    }
}