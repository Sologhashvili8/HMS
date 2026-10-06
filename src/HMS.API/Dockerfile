FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/HMS.API/HMS.API.csproj", "src/HMS.API/"]
COPY ["src/HMS.Application/HMS.Application.csproj", "src/HMS.Application/"]
COPY ["src/HMS.Domain/HMS.Domain.csproj", "src/HMS.Domain/"]
COPY ["src/HMS.Infrastructure/HMS.Infrastructure.csproj", "src/HMS.Infrastructure/"]
RUN dotnet restore "./src/HMS.API/HMS.API.csproj"
COPY . .
WORKDIR "/src/src/HMS.API"
RUN dotnet build "./HMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
RUN mkdir -p /app/wwwroot/images/uploads && chown -R $APP_UID /app/wwwroot/images/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "HMS.API.dll"]
