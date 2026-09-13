ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
WORKDIR /src
COPY Archive.Core/Archive.Core.csproj Archive.Core/
COPY Archive.Api/Archive.Api.csproj Archive.Api/
RUN dotnet restore Archive.Api/Archive.Api.csproj
COPY Archive.Core/ Archive.Core/
COPY Archive.Api/ Archive.Api/
RUN dotnet publish Archive.Api/Archive.Api.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-composite-extra AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Archive.Api.dll"]
