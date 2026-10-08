# cTab IRL web server (cTabWebApp), community "cloud" build as in cTabIRL/setup/setup.sh.
# Build from the repository root (needs the Arma3TacMap submodule):
#   git submodule update --init --recursive
#   docker build -t ctab-web .
#
# Configuration: mount an appsettings.Production.json at /app/appsettings.Production.json
# (SteamKey, Communauty, ...). Storage paths default to the volumes below.

# SDK 9 builds the net8.0 app; SDK 8's older NuGet resolver rejects the AngleSharp
# versions required by AspNet.Security.OpenId (>= 1.0.6) and HtmlSanitizer (= 0.17.1).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Restore first so package downloads are cached between code changes.
COPY Arma3TacMap/Arma3TacMapLibrary/Arma3TacMapLibrary.csproj Arma3TacMap/Arma3TacMapLibrary/
COPY cTabIRL/cTabWebApp/cTabWebApp.csproj cTabIRL/cTabWebApp/
RUN dotnet restore cTabIRL/cTabWebApp/cTabWebApp.csproj
COPY Arma3TacMap/ Arma3TacMap/
COPY cTabIRL/cTabWebApp/ cTabIRL/cTabWebApp/
RUN dotnet publish cTabIRL/cTabWebApp/cTabWebApp.csproj -c ReleaseCloud -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
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
