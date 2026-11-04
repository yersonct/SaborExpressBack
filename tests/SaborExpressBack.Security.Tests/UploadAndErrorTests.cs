using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Responses;
using SaborExpress.Shared.Security;
using Xunit;

namespace SaborExpressBack.Security.Tests;
public sealed class UploadAndErrorTests
{
    private static IFormFile File(byte[] bytes, string name, string type) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = type };

    [Theory]
    [InlineData("photo.html", "image/png")]
    [InlineData("photo.svg", "image/png")]
    [InlineData("photo.png", "text/html")]
    public void DisguisedUploadsAreRejected(string name, string contentType) =>
        Assert.Throws<ArgumentException>(() => UploadFilePolicy.ImageExtension(File("<script>alert(1)</script>"u8.ToArray(), name, contentType)));

    [Fact]
    public void SignatureDeterminesSafeImageExtension()
    {
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.Equal(".png", UploadFilePolicy.ImageExtension(File(signature, "PHOTO.PNG", "image/png")));
        Assert.Throws<ArgumentException>(() => UploadFilePolicy.ImageExtension(File(signature, "photo.html", "image/png")));
    }

    [Fact]
    public void PdfSignatureIsRequiredEvenWhenMimeSaysPdf()
    {
        Assert.Throws<ArgumentException>(() => UploadFilePolicy.ValidatePdf(File("<html>fake</html>"u8.ToArray(), "cv.pdf", "application/pdf")));
        UploadFilePolicy.ValidatePdf(File("%PDF-1.7\n%%EOF"u8.ToArray(), "cv.pdf", "application/pdf"));
    }

    [Fact]
    public void OversizedAndEmptyFilesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => UploadFilePolicy.ValidatePdf(File([], "cv.pdf", "application/pdf")));
        Assert.Throws<ArgumentException>(() => UploadFilePolicy.ValidatePdf(File(new byte[UploadFilePolicy.MaxFileBytes + 1], "cv.pdf", "application/pdf")));
    }

    [Fact]
    public void UnexpectedFailuresReturn500WithoutInternalDetails()
    {
        var http = new DefaultHttpContext { TraceIdentifier = "trace-test" };
        var context = new ExceptionContext(new ActionContext(http, new(), new()), [])
        { Exception = new Exception("private-database-detail") };
        new ApiExceptionFilter().OnException(context);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
        var error = Assert.IsType<ApiError>(result.Value);
        Assert.Equal("INTERNAL_ERROR", error.Code);
        Assert.Equal("trace-test", error.TraceId);
        Assert.DoesNotContain("private-database-detail", error.Message);
    }
    [Theory]
    [InlineData("/api/Orders", null, null)]
    [InlineData("/api/v1/Orders", 1, 100)]
    public void LegacyListsRemainUnboundedWhileV1DefaultsToPages(string path, int? expectedPage, int? expectedSize)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        var page = new PageQuery();
        var context = new ActionExecutingContext(new ActionContext(http, new(), new()), [],
            new Dictionary<string, object?> { ["filter"] = page }, new object());
        new ApiPaginationFilter().OnActionExecuting(context);
        Assert.Equal(expectedPage, page.Page);
        Assert.Equal(expectedSize, page.PageSize);
    }

}
