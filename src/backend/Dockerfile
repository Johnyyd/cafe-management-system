FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["CafeManagement.Backend.sln", "./"]
COPY ["src/CafeManagement.Domain/CafeManagement.Domain.csproj", "src/CafeManagement.Domain/"]
COPY ["src/CafeManagement.Application/CafeManagement.Application.csproj", "src/CafeManagement.Application/"]
COPY ["src/CafeManagement.Infrastructure/CafeManagement.Infrastructure.csproj", "src/CafeManagement.Infrastructure/"]
COPY ["src/CafeManagement.Api/CafeManagement.Api.csproj", "src/CafeManagement.Api/"]
COPY ["tests/CafeManagement.Domain.Tests/CafeManagement.Domain.Tests.csproj", "tests/CafeManagement.Domain.Tests/"]
COPY ["tests/CafeManagement.Application.Tests/CafeManagement.Application.Tests.csproj", "tests/CafeManagement.Application.Tests/"]
COPY ["tests/CafeManagement.IntegrationTests/CafeManagement.IntegrationTests.csproj", "tests/CafeManagement.IntegrationTests/"]
COPY ["tests/CafeManagement.ArchitectureTests/CafeManagement.ArchitectureTests.csproj", "tests/CafeManagement.ArchitectureTests/"]

RUN dotnet restore "CafeManagement.Backend.sln"

# Copy everything else and build
COPY . .
WORKDIR "/src"
RUN dotnet build "CafeManagement.Backend.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CafeManagement.Backend.sln" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CafeManagement.Api.dll"]
