# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AgentAtlas.slnx .
COPY src/Atlas.Host/Atlas.Host.csproj src/Atlas.Host/

# Restore
RUN dotnet restore src/Atlas.Host/Atlas.Host.csproj

# Copy source
COPY src/Atlas.Host/ src/Atlas.Host/

# Build and publish
RUN dotnet publish src/Atlas.Host/Atlas.Host.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create catalog mount point
RUN mkdir -p /catalog

# Copy published app
COPY --from=build /app/publish .

# Default environment
ENV ASPNETCORE_URLS=http://+:8080
ENV Atlas__CatalogPath=/catalog

EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "Atlas.Host.dll"]
