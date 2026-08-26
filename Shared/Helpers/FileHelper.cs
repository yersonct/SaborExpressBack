namespace SaborExpress.Shared.Helpers
{
    public static class FileHelper
    {
        public static async Task<byte[]> ToByteArrayAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
