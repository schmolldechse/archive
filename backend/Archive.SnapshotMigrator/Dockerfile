ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
WORKDIR /src
COPY Archive.Core/Archive.Core.csproj Archive.Core/
COPY Archive.SnapshotMigrator/Archive.SnapshotMigrator.csproj Archive.SnapshotMigrator/
RUN dotnet restore Archive.SnapshotMigrator/Archive.SnapshotMigrator.csproj
COPY Archive.Core/ Archive.Core/
COPY Archive.SnapshotMigrator/ Archive.SnapshotMigrator/
RUN dotnet publish Archive.SnapshotMigrator/Archive.SnapshotMigrator.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-composite-extra AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Archive.SnapshotMigrator.dll"]
