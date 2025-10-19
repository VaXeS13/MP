# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

WORKDIR /src

# Copy solution file and project files
COPY ["MP.sln", "./"]
COPY ["src/", "src/"]
COPY ["test/", "test/"]

# Restore and build
RUN dotnet restore "MP.sln"
RUN dotnet build "MP.sln" --configuration Release --no-restore

# Publish stage
FROM builder AS publisher

WORKDIR /src

# Publish the main API project
RUN dotnet publish "src/MP.HttpApi.Host/MP.HttpApi.Host.csproj" \
    --configuration Release \
    --no-restore \
    --no-build \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -u 1001 appuser

WORKDIR /app

# Copy published files
COPY --from=publisher --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health-status || exit 1

# Environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Run application
ENTRYPOINT ["dotnet", "MP.HttpApi.Host.dll"]