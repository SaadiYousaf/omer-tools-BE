// public interface IFileStorageService
// {
//     Task<string> SaveFileAsync(IFormFile file, string containerName);
//     Task DeleteFileAsync(string filePath, string containerName);
//     string GetFileUrl(string fileName, string containerName);
// }

// public class LocalFileStorageService : IFileStorageService
// {
//     private readonly IWebHostEnvironment _env;
//     private readonly IHttpContextAccessor _httpContextAccessor;

//     public LocalFileStorageService(
//         IWebHostEnvironment env,
//         IHttpContextAccessor httpContextAccessor
//     )
//     {
//         _env = env;
//         _httpContextAccessor = httpContextAccessor;
//     }

//     public async Task<string> SaveFileAsync(IFormFile file, string containerName)
//     {
//         var extension = Path.GetExtension(file.FileName);
//         var fileName = $"{Guid.NewGuid()}{extension}";
//         var folder = Path.Combine(_env.WebRootPath, containerName);

//         if (!Directory.Exists(folder))
//         {
//             Directory.CreateDirectory(folder);
//         }

//         var filePath = Path.Combine(folder, fileName);
//         using (var stream = new FileStream(filePath, FileMode.Create))
//         {
//             await file.CopyToAsync(stream);
//         }

//         return fileName;
//     }

//     public async Task DeleteFileAsync(string fileName, string containerName)
//     {
//         var filePath = Path.Combine(_env.WebRootPath, containerName, fileName);
//         if (File.Exists(filePath))
//         {
//             await Task.Run(() => File.Delete(filePath));
//         }
//     }

//     public string GetFileUrl(string fileName, string containerName)
//     {
//         var request = _httpContextAccessor.HttpContext.Request;
//         return $"{request.Scheme}://{request.Host}/{containerName}/{fileName}";
//     }
// }
