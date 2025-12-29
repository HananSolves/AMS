# ----------- BUILD STAGE -----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and projects
COPY AttendanceManagementSystem.sln ./
COPY AMS.Core/ AMS.Core/
COPY AMS.Application/ AMS.Application/
COPY AMS.Infrastructure/ AMS.Infrastructure/
COPY AMS.Web/ AMS.Web/

# Restore NuGet packages
RUN dotnet restore

# Build the solution in Release mode
RUN dotnet build -c Release --no-restore

# Publish Web project
RUN dotnet publish AMS.Web/AMS.Web.csproj -c Release -o /app/publish --no-build

# ----------- RUNTIME STAGE -----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Optional: set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV ASPNETCORE_ENVIRONMENT=Production
# Render provides PORT environment variable automatically
ENV ASPNETCORE_URLS=http://*:${PORT:-1000}

# Expose port (Render uses PORT environment variable)
EXPOSE 1000

# Entry point
ENTRYPOINT ["dotnet", "AMS.Web.dll"]
