# =============================================================================
# OrbitOS API Dockerfile
# =============================================================================
# Multi-stage build for .NET 8 API
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY src/OrbitOS.Api/OrbitOS.Api.csproj src/OrbitOS.Api/
COPY src/OrbitOS.Application/OrbitOS.Application.csproj src/OrbitOS.Application/
COPY src/OrbitOS.Infrastructure/OrbitOS.Infrastructure.csproj src/OrbitOS.Infrastructure/
COPY src/OrbitOS.Domain/OrbitOS.Domain.csproj src/OrbitOS.Domain/
COPY src/OrbitOS.Contracts/OrbitOS.Contracts.csproj src/OrbitOS.Contracts/

RUN dotnet restore src/OrbitOS.Api/OrbitOS.Api.csproj

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/OrbitOS.Api/OrbitOS.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:5027
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5027

# Copy published app
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5027/health || exit 1

ENTRYPOINT ["dotnet", "OrbitOS.Api.dll"]
