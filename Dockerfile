# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["AMS.Core/AMS.Core.csproj", "AMS.Core/"]
COPY ["AMS.Application/AMS.Application.csproj", "AMS.Application/"]
COPY ["AMS.Infrastructure/AMS.Infrastructure.csproj", "AMS.Infrastructure/"]
COPY ["AMS.Web/AMS.Web.csproj", "AMS.Web/"]

# Restore dependencies
RUN dotnet restore "AMS.Web/AMS.Web.csproj"

# Copy everything else
COPY . .

# Build Tailwind CSS
WORKDIR /src/AMS.Web
RUN apt-get update && apt-get install -y nodejs npm
RUN npm install
RUN npm run build:css

# Build application
WORKDIR /src
RUN dotnet build "AMS.Web/AMS.Web.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "AMS.Web/AMS.Web.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published app
COPY --from=publish /app/publish .

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AMS.Web.dll"]
```

### 3.3 Create .dockerignore

Create `.dockerignore` in the root directory:
```
**/bin/
**/obj/
**/out/
**/node_modules/
**/.vs/
**/.vscode/
**/.idea/
**/publish/
**/*.user
**/.DS_Store
**/Thumbs.db
**/.git/
**/wwwroot/css/site.css
