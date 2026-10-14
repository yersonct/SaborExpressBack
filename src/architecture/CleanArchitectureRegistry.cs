namespace SaborExpress.Architecture;

public static class CleanArchitectureRegistry
{
    public const string Presentation = "Presentation";
    public const string Application = "Application";
    public const string Domain = "Domain";
    public const string Infrastructure = "Infrastructure";

    public static readonly string[] Features =
    [
        "Orders",
        "Products",
        "Employees",
        "Notifications",
        "UserPreferences",
        "Branches",
        "Payments",
        "Tables",
        "Reviews",
        "Deliveries"
    ];

    public static readonly string[] EntryPoints =
    [
        "HTTP controllers",
        "API endpoints",
        "SignalR hubs",
        "Background services",
        "Feature services"
    ];

    public static readonly string[] Principles =
    [
        "Controller / screen / view only receives and dispatches.",
        "Service or hook owns business logic.",
        "Infrastructure owns data access, auth and realtime transport.",
        "Each feature remains encapsulated behind its own domain boundary."
    ];
}
