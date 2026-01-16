FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AuthCore.API/AuthCore.API.csproj", "src/AuthCore.API/"]
COPY ["src/AuthCore.Core/AuthCore.Core.csproj", "src/AuthCore.Core/"]
COPY ["src/AuthCore.Infrastructure/AuthCore.Infrastructure.csproj", "src/AuthCore.Infrastructure/"]
RUN dotnet restore "src/AuthCore.API/AuthCore.API.csproj"
COPY . .
WORKDIR "/src/src/AuthCore.API"
RUN dotnet build "AuthCore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthCore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthCore.API.dll"]
