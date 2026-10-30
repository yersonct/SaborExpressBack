namespace SaborExpress.Shared.Security;

public static class UploadFilePolicy
{
    public const long MaxFileBytes = 5 * 1024 * 1024;

    private static byte[] Header(IFormFile file, int length)
    {
        if (file.Length <= 0 || file.Length > MaxFileBytes)
            throw new ArgumentException("El archivo debe contener datos y no superar 5MB.");
        using var stream = file.OpenReadStream();
        var bytes = new byte[length];
        var count = 0;
        while (count < length)
        {
            var read = stream.Read(bytes, count, length - count);
            if (read == 0) break;
            count += read;
        }
        return bytes[..count];
    }

    public static void ValidatePdf(IFormFile file)
    {
        var header = Header(file, 5);
        if (!header.AsSpan().SequenceEqual("%PDF-"u8) ||
            !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El CV debe ser un archivo PDF válido.");
    }

    public static string ImageExtension(IFormFile file)
    {
        var header = Header(file, 12);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (header.Length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) &&
            extension == ".png" && file.ContentType == "image/png") return ".png";
        if (header.Length >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255 &&
            (extension == ".jpg" || extension == ".jpeg") && file.ContentType == "image/jpeg") return ".jpg";
        if (header.Length == 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8) && extension == ".webp" && file.ContentType == "image/webp") return ".webp";
        throw new ArgumentException("La foto debe ser un archivo JPG, PNG o WEBP válido.");
    }
}
