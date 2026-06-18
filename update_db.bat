cd ShowDanWebApi
dotnet ef migrations add InitialFullSchema6 --project ShowDanWebApi.Data --startup-project ShowDanWebApi.API
dotnet ef database update --project ShowDanWebApi.Data --startup-project ShowDanWebApi.API