# cTab IRL web server (cTabWebApp), community "cloud" build as in cTabIRL/setup/setup.sh.
# Build from the repository root (needs the Arma3TacMap submodule):
#   git submodule update --init --recursive
#   docker build -t ctab-web .
#
# Configuration: mount an appsettings.Production.json at /app/appsettings.Production.json
# (SteamKey, Communauty, ...). Storage paths default to the volumes below.

# Base images are pinned by digest (Dependabot's docker ecosystem keeps them current).
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /src
# Restore first so package downloads are cached between code changes.
COPY Arma3TacMap/Arma3TacMapLibrary/Arma3TacMapLibrary.csproj Arma3TacMap/Arma3TacMapLibrary/
COPY cTabIRL/cTabWebApp/cTabWebApp.csproj cTabIRL/cTabWebApp/
RUN dotnet restore cTabIRL/cTabWebApp/cTabWebApp.csproj
COPY Arma3TacMap/ Arma3TacMap/
COPY cTabIRL/cTabWebApp/ cTabIRL/cTabWebApp/
RUN dotnet publish cTabIRL/cTabWebApp/cTabWebApp.csproj -c ReleaseCloud -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac
WORKDIR /app
COPY --from=build /out ./
# Writable locations for the non-root `app` user: shared photos, session recordings,
# and ASP.NET data-protection keys (keeps Steam sign-ins valid across restarts).
RUN mkdir -p /data/images /data/recordings /keys && chown -R app:app /data /keys
USER app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    UnixKeysDirectory=/keys \
    Images__StorageLocation=/data/images \
    Recordings__StorageLocation=/data/recordings
VOLUME ["/data", "/keys"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "cTabWebApp.dll"]
