ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
WORKDIR /src
COPY Archive.Core/Archive.Core.csproj Archive.Core/
COPY Archive.SnapshotMigration/Archive.SnapshotMigration.csproj Archive.SnapshotMigration/
RUN dotnet restore Archive.SnapshotMigration/Archive.SnapshotMigration.csproj
COPY Archive.Core/ Archive.Core/
COPY Archive.SnapshotMigration/ Archive.SnapshotMigration/
RUN dotnet publish Archive.SnapshotMigration/Archive.SnapshotMigration.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_ENVIRONMENT=Production
USER $APP_UID
ENTRYPOINT ["dotnet", "Archive.SnapshotMigration.dll"]
