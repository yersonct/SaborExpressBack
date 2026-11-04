using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Localization;
using SaborExpress.Shared.Responses;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class ApiErrorResultFilterTests
{
    [Fact]
    public void DirectBadRequestPayloadBecomesTheStandardApiError()
    {
        var context = Context(new BadRequestObjectResult(new { message = "El archivo no es válido." }));

        new ApiErrorResultFilter(new ApiMessageLocalizer()).OnResultExecuting(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(400, result.StatusCode);
        var error = Assert.IsType<ApiError>(result.Value);
        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.Equal("El archivo no es válido.", error.Message);
        Assert.Equal("contract-trace", error.TraceId);
        Assert.Equal(error.Message, error.Error);
    }

    [Fact]
    public void DirectNotFoundPayloadBecomesTheStandardApiError()
    {
        var context = Context(new NotFoundObjectResult(new { error = "El empleado no tiene CV cargado." }));

        new ApiErrorResultFilter(new ApiMessageLocalizer()).OnResultExecuting(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(404, result.StatusCode);
        var error = Assert.IsType<ApiError>(result.Value);
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal("El empleado no tiene CV cargado.", error.Message);
        Assert.Equal("contract-trace", error.TraceId);
    }

    [Fact]
    public void DirectServerErrorDoesNotExposeItsPayload()
    {
        var context = Context(new ObjectResult(new { detail = "private database connection" }) { StatusCode = 500 });

        new ApiErrorResultFilter(new ApiMessageLocalizer()).OnResultExecuting(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
        var error = Assert.IsType<ApiError>(result.Value);
        Assert.Equal("INTERNAL_ERROR", error.Code);
        Assert.Equal("No se pudo completar la solicitud.", error.Message);
        Assert.DoesNotContain("private database", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("contract-trace", error.TraceId);
    }

    [Fact]
    public void SuccessAndAlreadyNormalizedErrorsArePreserved()
    {
        var filter = new ApiErrorResultFilter(new ApiMessageLocalizer());
        var success = Context(new OkObjectResult(new { value = 1 }));
        filter.OnResultExecuting(success);
        Assert.IsType<OkObjectResult>(success.Result);

        var apiError = new ApiError("NOT_FOUND", "El recurso no existe.", "existing-trace");
        var normalized = Context(new NotFoundObjectResult(apiError));
        filter.OnResultExecuting(normalized);
        Assert.Same(apiError, Assert.IsAssignableFrom<ObjectResult>(normalized.Result).Value);
    }

    private static ResultExecutingContext Context(IActionResult result)
    {
        var http = new DefaultHttpContext { TraceIdentifier = "contract-trace" };
        return new ResultExecutingContext(new ActionContext(http, new(), new()), [], result, new object());
    }
}
