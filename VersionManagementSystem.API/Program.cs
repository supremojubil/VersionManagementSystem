using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Core.Services;
using VersionManagementSystem.Infrastructure.Data;
using VersionManagementSystem.Infrastructure.Repositories;
using VersionManagementSystem.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database — Pomelo/MySQL provider (matches the MySQL infrastructure already used).
// Swap AddDbContext's UseMySql call for UseSqlServer if the target environment uses SQL Server instead.
var connectionString = builder.Configuration.GetConnectionString("VersionManagementDb") ?? throw new InvalidOperationException("Connection string 'VersionManagementDb' was not found.");

builder.Services.AddDbContext<VersionManagementDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Repositories
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IApplicationVersionRepository, ApplicationVersionRepository>();
builder.Services.AddScoped<IReleaseNoteRepository, ReleaseNoteRepository>();
builder.Services.AddScoped<IUpdatePackageRepository, UpdatePackageRepository>();
builder.Services.AddScoped<IClientInstallationRepository, ClientInstallationRepository>();
builder.Services.AddScoped<IUpdateHistoryRepository, UpdateHistoryRepository>();

// Package storage — local disk under PackageStorage:RootPath (see appsettings.json).
builder.Services.Configure<PackageStorageOptions>(builder.Configuration.GetSection("PackageStorage"));
builder.Services.AddSingleton<IPackageStorageService, LocalPackageStorageService>();

// Domain services
builder.Services.AddSingleton<IVersionService, VersionService>();
builder.Services.AddSingleton<IChecksumService, ChecksumService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationVersionService, ApplicationVersionService>();
builder.Services.AddScoped<IReleaseService, ReleaseService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IClientTrackingService, ClientTrackingService>();
builder.Services.AddScoped<IUpdateCheckService, UpdateCheckService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Large update packages (installers) can exceed ASP.NET Core's default 30 MB multipart body limit.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
    options.MultipartBodyLengthLimit = 500L * 1024 * 1024; // 500 MB
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
