ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
WORKDIR /src
COPY Archive.Core/Archive.Core.csproj Archive.Core/
COPY Archive.Worker/Archive.Worker.csproj Archive.Worker/
RUN dotnet restore Archive.Worker/Archive.Worker.csproj
COPY Archive.Core/ Archive.Core/
COPY Archive.Worker/ Archive.Worker/
RUN dotnet publish Archive.Worker/Archive.Worker.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV DOTNET_ENVIRONMENT=Production \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates wget \
    && wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends powershell xvfb xauth \
    && pwsh ./playwright.ps1 install --with-deps --no-shell chromium \
    && apt-get purge -y --auto-remove powershell wget packages-microsoft-prod \
    && rm -rf /var/lib/apt/lists/* /root/.cache /tmp/*
ENTRYPOINT ["xvfb-run", "-a", "--server-args=-screen 0 1440x900x24", "dotnet", "Archive.Worker.dll"]
