using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Responses;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class ApiErrorContractTests
{
    [Fact]
    public void UnexpectedInvalidOperationIsSanitizedAsServerError()
    {
        var error = Apply(new InvalidOperationException("internal database detail"));
        Assert.Equal("INTERNAL_ERROR", error.Code);
        Assert.Equal("No se pudo completar la solicitud.", error.Message);
        Assert.DoesNotContain("database", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TypedBusinessConflictRemainsAConflict()
    {
        var error = Apply(new ConflictException("El turno ya está cerrado."));
        Assert.Equal("CONFLICT", error.Code);
        Assert.Equal("El turno ya está cerrado.", error.Message);
    }

    [Fact]
    public void ExplicitPermissionDenialRemainsForbidden()
    {
        var error = Apply(new ForbiddenException("No tienes permiso para esta acción."));
        Assert.Equal("FORBIDDEN", error.Code);
        Assert.Equal("No tienes permiso para esta acción.", error.Message);
    }

    private static ApiError Apply(Exception exception)
    {
        var http = new DefaultHttpContext { TraceIdentifier = "contract-test" };
        var context = new ExceptionContext(new ActionContext(http, new(), new()), [])
        { Exception = exception };
        new ApiExceptionFilter().OnException(context);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(exception is ConflictException ? 409 : exception is ForbiddenException ? 403 : 500, result.StatusCode);
        return Assert.IsType<ApiError>(result.Value);
    }
}