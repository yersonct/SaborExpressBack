using SaborExpress.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddDependencies();

builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

app.UseSwaggerConfiguration();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();