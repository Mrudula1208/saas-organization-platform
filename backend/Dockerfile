# ==============================================================================
# Multi-stage Dockerfile for SaaS Organization Platform .NET 8 Backend API
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for optimized layer caching
COPY ["SaaSPlatform.API/SaaSPlatform.API.csproj", "SaaSPlatform.API/"]
COPY ["SaaSPlatform_DataAccess/SaaSPlatform.Application.csproj", "SaaSPlatform_DataAccess/"]
COPY ["SaaSPlatform_Model/SaaSPlatform.Domain.csproj", "SaaSPlatform_Model/"]
COPY ["SaaSPlatform_Utility/SaaSPlatform_Utility.csproj", "SaaSPlatform_Utility/"]
COPY ["SaaSOrganizationPlatform/SaaSPlatform.Infrastructure/SaaSPlatform.Infrastructure.csproj", "SaaSOrganizationPlatform/SaaSPlatform.Infrastructure/"]

# Restore NuGet dependencies
RUN dotnet restore "SaaSPlatform.API/SaaSPlatform.API.csproj"

# Copy all source files
COPY . .

# Build and publish release binaries
WORKDIR "/src/SaaSPlatform.API"
RUN dotnet publish "SaaSPlatform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Set production environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Create uploads storage directory
RUN mkdir -p /app/wwwroot/uploads

# Copy compiled application from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Start the API service
ENTRYPOINT ["dotnet", "SaaSPlatform.API.dll"]
