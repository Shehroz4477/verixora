# ============================================================
# VERIXORA – Dockerfile
# ============================================================
# Summary:
#   Multi-stage build:
#     Stage 1 (build):   Compiles the ApiHost and all modules
#                         using the .NET 8 SDK image.
#     Stage 2 (runtime): Copies only the published output into
#                         a lean ASP.NET 8 runtime image.
#
#   Why: The SDK image is large (700MB+) and includes compilers
#        that are a security risk in production. The runtime
#        image is much smaller (~200MB) and contains only what
#        is needed to run. This follows container best practices
#        and reduces the attack surface (ADR-032).
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the files needed for NuGet restore first.
# This leverages Docker layer caching: if these files don't change,
# the restore step is skipped on rebuild, saving minutes per build.
COPY *.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY global.json .
COPY nuget.config .

# Copy all project files (but not source yet) for restore.
# The trailing /* ensures each .csproj ends up in the right folder.
COPY src/ApiHost/ApiHost.csproj src/ApiHost/
COPY src/SharedKernel/SharedKernel.Domain/SharedKernel.Domain.csproj src/SharedKernel/SharedKernel.Domain/
COPY src/SharedKernel/SharedKernel.Application/SharedKernel.Application.csproj src/SharedKernel/SharedKernel.Application/
COPY src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/

# Restore NuGet packages. This is cached until any .csproj changes.
RUN dotnet restore src/ApiHost/ApiHost.csproj

# Now copy the entire source tree (all modules, tests, etc.).
# Build will re-run only if source files changed.
COPY . .

# Publish the ApiHost in Release mode to /app.
# --no-restore is safe because we restored above.
RUN dotnet publish src/ApiHost/ApiHost.csproj -c Release -o /app --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Expose HTTP (80) and HTTPS (443) ports.
# These are only documentation; actual mapping is in docker-compose or K8s.
EXPOSE 80
EXPOSE 443

# Copy the published output from the build stage.
# The COPY --from=build creates a clean image with only the app binaries.
COPY --from=build /app .

# Run as a non-root user for security.
# The 'app' user is built into the ASP.NET runtime image.
USER app

# The entry point: run the compiled ApiHost.dll.
ENTRYPOINT ["dotnet", "ApiHost.dll"]
