using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SaborExpress.Configuration;

// Existing installed clients retain /api; new integrations use /api/v1.
public sealed class ApiVersionConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        foreach (var selector in controller.Selectors.ToArray())
        {
            var route = selector.AttributeRouteModel?.Template;
            if (route is null || !route.StartsWith("api/", StringComparison.OrdinalIgnoreCase)) continue;
            var versioned = new SelectorModel(selector);
            versioned.AttributeRouteModel!.Template = "api/v1/" + route[4..];
            controller.Selectors.Add(versioned);
        }
    }
}
