<p align="center"><img src="cTabIRL/cTabWebApp/wwwroot/tlb/tlb-favicon.png" width="72" alt="TLB MilSim"></p>

<h1 align="center">TLB MilSim · cTab</h1>

<p align="center">Your in-game cTab outside the game: live on a phone, tablet or second monitor.</p>

> [!NOTE]
> **This is TLB MilSim's fork of [jetelain/cTab](https://github.com/jetelain/cTab).** We changed the **cTab IRL web server** (`cTabIRL/cTabWebApp`) to match the TLB style and workflow: the TLB look and top bar, the shared TLB sign-in, Docker hosting and a weekly upstream check. The Arma 3 mods are not changed.
>
> Our work is on the **`tlb`** branch (the default); `master` follows upstream unchanged. The original project's README is kept [below](#original-readme).

## Part of the TLB MilSim suite

The Lost Battalion's web tools are five repositories that run together as one system: one sign-in, one top bar, one look. [HubTLB](https://github.com/TLB-MilSim/HubTLB) ties them together and runs them with Docker Compose.

| Part | Repository | What it does | Live address | Local |
|---|---|---|---|---|
| Hub | [TLB-MilSim/HubTLB](https://github.com/TLB-MilSim/HubTLB) (private) | Launcher page, shared top bar and theme, the Docker Compose stack | hub.tlbmilsim.com | `localhost:8000` |
| Sign-in | [TLB-MilSim/TLB-SSO](https://github.com/TLB-MilSim/TLB-SSO) (private) | TLB Login Service: one Discord sign-in for every site | auth.tlbmilsim.com | `localhost:5100` |
| **cTab** | **[TLB-MilSim/cTab](https://github.com/TLB-MilSim/cTab)** (this repository), fork of [jetelain/cTab](https://github.com/jetelain/cTab) | Your in-game cTab on a phone, tablet or second monitor | ctab.tlbmilsim.com | `localhost:5010` |
| Tac Map | [TLB-MilSim/TLB-Tac-Map](https://github.com/TLB-MilSim/TLB-Tac-Map) (private) | Plan operations together on a live map | tacmap.tlbmilsim.com | `localhost:8080` |
| OCAP | [TLB-MilSim/ocap-web](https://github.com/TLB-MilSim/ocap-web), fork of [OCAP2/web](https://github.com/OCAP2/web) | Replays of past operations | ocap.tlbmilsim.com | `localhost:5000` |

The live addresses are where the sites go when the suite is deployed; locally each one runs on its own port.

## What we changed

Everything is in the web app, `cTabIRL/cTabWebApp`, plus a few repository files. Where possible the changes are in files of our own, so upstream updates merge cleanly.

**TLB look**
- `wwwroot/tlb/`: the shared TLB top bar (`tlb-suite-bar.js/.css`), theme colours (`tlb-theme.css`) and favicon. The master copies are in HubTLB (`hub/suite/`), and `hub/sync-suite.ps1` copies them here. Don't edit them here.
- `wwwroot/css/site.css`: the TLB theme for cTab's own pages.
- `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml`, `Map.cshtml`, `EFIS.cshtml`, `Views/Session/Replay.cshtml`: the top bar, the TLB header, page titles in the form `TLB MilSim · cTab · <page>`, and wording that describes cTab as "outside the game" (phone, tablet or second monitor), with French translations in the `.resx` files.

**Shared TLB sign-in** (the [TLB Login Service](https://github.com/TLB-MilSim/TLB-SSO))
- `TlbAuthSettings.cs`: the `TlbAuth` settings. When `TlbAuth:PublicUrl` is set, cTab uses the TLB sign-in instead of its own Steam sign-in; when it's empty, cTab works exactly like upstream.
- `Startup.cs`: reads the TLB session cookie (`tlb_session`, an ES256 JWT). It checks the signature with the Login Service's public key, plus issuer, audience (`tlb-suite`) and expiry (30 seconds clock skew), and requires the `member` permission.
- `Services/TlbVerifyService.cs`: also asks the Login Service's live `/verify` endpoint about every session, cached for 30 seconds. Signing out, a block or a removed Discord role reaches cTab within 30 seconds, and the Steam ID used is the one linked right now. If the Login Service can't be reached, there is no access (fails closed).
- `SteamHelper.cs`: takes the Steam ID from the TLB account's linked Steam (`steam_id`).
- `Controllers/HomeController.cs`, `Views/Home/Index.cshtml`: **Sign in with Discord**, **Link Steam account** and the cTab key form; signing out goes through TLB, so it signs out of the whole suite.

**Hosting**
- `Dockerfile`, `.dockerignore`: builds the server image on .NET 10. The base images are pinned by digest, and the app runs as the image's normal `app` user with `/data` (photos, recordings) and `/keys` (data-protection keys) as volumes.
- `Services/PublicUriService.cs`: the connect page shows the address the browser used, not the container's internal listen address.
- `cTabWebApp.csproj`: .NET 10 (upstream is on .NET 8, which reaches end of support on 2026-11-10), ASP.NET packages 10.0.x, and AngleSharp 1.8.5 for a security fix.

**Tests:** `cTabWebAppTest/TlbAuthSettingsTest.cs` (sign-in settings) and `MarkerSanitizerTest.cs` (map notes are still sanitized with the newer AngleSharp).

**Repository**
- `.github/workflows/upstream-sync.yml`: the weekly upstream check (see [Taking upstream updates](#taking-upstream-updates)).
- `.github/dependabot.yml`: weekly updates for NuGet packages, base images and actions.
- `.gitignore`: `appsettings.Production.json` is never committed.
- Upstream's release workflows (`.NET Release`, `Mods Release`) are disabled on this fork.

## Run it

cTab runs as the `ctab` service in [HubTLB](https://github.com/TLB-MilSim/HubTLB)'s Docker Compose stack, together with the hub, the Login Service and OCAP. Clone this repository into HubTLB's `cTab/` folder (it needs the `Arma3TacMap` submodule):

```bash
git clone -b tlb --recurse-submodules https://github.com/TLB-MilSim/cTab.git cTab
```

Then, from HubTLB, `docker compose up -d --build ctab`. The site is on http://localhost:5010.

To build only the image: `docker build -t tlb-ctab .` from this repository's root.

**In Arma 3:** set the CBA setting **Server URL** (`ctab_irl_connect_uri`) to the site address followed by `/hub`: locally `http://localhost:5010/hub`, live `https://ctab.tlbmilsim.com/hub`.

**For members:**
1. Sign in with Discord (once for the whole suite). The first time, link your Steam account.
2. In the game, open cTab IRL to get your 6-digit key.
3. Enter the key on the site, or scan the QR code shown in the game.

## Configuration

| Setting | Where | Meaning |
|---|---|---|
| `TlbAuth__PublicUrl` | environment | The Login Service's public address, also the token issuer. Locally `http://localhost:5100/`, live `https://auth.tlbmilsim.com/`. Empty = cTab's own Steam sign-in. |
| `TlbAuth__MetadataAddress` | environment | Where cTab reads the Login Service's signing keys. Default: `<PublicUrl>.well-known/tlb-suite-configuration`. In Docker: `http://auth:8080/.well-known/tlb-suite-configuration`. |
| `TlbAuth__VerifyAddress` | environment | The live session check. Default: `<PublicUrl>verify`. In Docker: `http://auth:8080/verify`. |
| `Communauty` | `appsettings.Production.json` | The TLB details shown on the site (name, website, contact, legal names). |
| `SteamKey` | `appsettings.Production.json` | Steam Web API key, only used by cTab's own Steam sign-in (when `TlbAuth__PublicUrl` is empty). |
| `Images__StorageLocation`, `Recordings__StorageLocation`, `UnixKeysDirectory` | environment (set in the `Dockerfile`) | `/data/images`, `/data/recordings`, `/keys`. |

The Login Service addresses must use https, except internal ones (localhost, private IP addresses, Docker service names such as `auth`). In HubTLB, `appsettings.Production.json` is `config/ctab/appsettings.Production.json` (copy it from the `.example` next to it) and is mounted read-only into the container.

## Sign-in and permissions

The Login Service maps Discord roles to permissions; cTab only checks the permission name.

| Permission | Discord role | In cTab |
|---|---|---|
| `member` | `605409199219933194`, or any role that gives a TLB permission | Use cTab |

## Development

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build cTabIRL/cTabWebApp/cTabWebApp.csproj -c ReleaseCloud
dotnet test cTabIRL/cTabWebAppTest/cTabWebAppTest.csproj
```

Without the .NET SDK, run the same commands in the `mcr.microsoft.com/dotnet/sdk:10.0` container.

## Taking upstream updates

`origin` is this fork and `upstream` is [jetelain/cTab](https://github.com/jetelain/cTab). In a clone, set `upstream` up as fetch-only so nothing is pushed there by mistake:

```bash
git remote add upstream https://github.com/jetelain/cTab.git
git remote set-url --push upstream no-push
```

**Weekly check:** the *Upstream sync* workflow runs every Monday at 06:00 UTC, and can also be run from the Actions tab.
- **No new commits:** nothing happens.
- **New commits that merge cleanly:** it builds and tests the merge, pushes the `upstream-sync` branch and opens an issue with a one-click **Create the PR** link (the TLB-MilSim organization doesn't let Actions open PRs).
- **New commits that change upstream's workflow files:** GitHub doesn't let Actions push those, so the issue lists the commands to merge from your clone.
- **Merge conflicts:** the issue lists the conflicting files.

Nothing is merged into `tlb` automatically. The workflow never runs upstream's code with write access: the merge, the tests and the push run in separate jobs.

**By hand:**

```bash
git fetch upstream
git switch -c update/upstream origin/tlb
git merge upstream/master        # resolve conflicts, build, test
git push -u origin update/upstream
```

Then open a PR into `tlb`. If `README.md` conflicts, keep this TLB part at the top and take upstream's version below the divider.

## Security

- The site only accepts TLB sessions that are valid right now (see `TlbVerifyService`).
- Known open item: `SixLabors.ImageSharp` 3.1.11 (used through the `Arma3TacMap` submodule) has published advisories, fixed only in 4.x. In cTab they could let a signed-in player crash photo processing with a crafted image. It needs the submodule to move to ImageSharp 4.

---

<a id="original-readme"></a>

*Everything below is the original project's README ([jetelain/cTab](https://github.com/jetelain/cTab)), unchanged.*

cTab+
=====
**Commander's Tablet - FBCB2, Blue Force Tracker, UAV, Helmet Cam and Messaging Interface**
*Created by Riouken*

Documentation
- For users, see [Install and Key bindings](https://github.com/jetelain/cTab/wiki/User-Documentation)
- For mission makers, see [Mission Makers and Server Settings](https://github.com/jetelain/cTab/wiki/Mission-Makers-Documentation)
- [Contributors and special thanks](AUTHORS.md)
- [Changelog](CHANGELOG.md)
- [License](LICENSE) : GNU GPL v2

Features
--------
+ Commander's tablet
+ Working FBCB2 Blue Force Tracker system
+ User placed makers - place markers for enemy, medical and general purpose
+ Tracks all crewed Bluefor Military vehicles
+ Tracks any dismounted troops with the proper equipment
+ Android based Blue Force Tracker
+ MicroDAGR hand-held GPS with Blue Force Tracking
+ Commanders Tablet can view live UAV streams
+ Commanders Tablet can view live Helmet Cam streams
+ Vehicle mounted FBCB2 interface, Blue Force Tracking
+ Tactical Awareness Display (TAD) for air vehicles, Blue Force Tracking
+ This system is available to only one side at a time, there is a mission configurable parameter to choose sides
+ None of the markers or icons show on maps, need one of the devices to view
+ Settings for mission makers to adjust features, and make them more realistic
+ cTab on your mobile device with additional [cTab Connect](https://github.com/jetelain/cTab/wiki/cTab-Connect) mod

Known Issues
------------
+ Switching to or from the full screen views while in a vehicle can cause issues (fixed by exiting vehicle)
  [*BIS issue with command, please vote for a fix*](http://feedback.arma3.com/view.php?id=11577)
+ If your are viewing yourself from the UAV or Helmet Cam in PiP screen, your textures can bug on your unit
+ Even though items go into the GPS slot they are not required to be there for cTab to operate, they can go anywhere in your inventory, i.e. your vest or uniform
+ Players that are experiencing conflicts with help screens (uses `H`as a key as well) are advised to rebind cTab `IF_MAIN`, for example to `SHIFT`+ `H`. This used to be an issue with Zeus but has been resolved as of cTab version 2.1. There might be other such cases though.
+ Helicopter pilots (and co-pilots) that are using RAVEN's LIFTER mod are advised to rebind cTab `IF_MAIN` to something other than the default as `H` is used by that mod and cannot be changed (as of this writing).
+ When a UAV is being actively piloted and a cTab user is already connected to the UAV's gunner turret, it is currently impossible to detect that there is a gunner connected in order to prevent a second player to connect to the same gunner turret. Wonky things happen to the player in the gunner seat if a second player connects and it might break the game. [Please vote for a fix](http://feedback.arma3.com/view.php?id=23693).

Required
--------
+ [CBA_A3 3.15 or later](https://github.com/CBATeam/CBA_A3/releases/latest), also available on [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=450814997)

Optional
--------
+ [ACE3](http://ace3mod.com/), also available on [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=463939057)

Media
-----
[![Tablet BFT](http://i.imgur.com/HnHLiv7m.jpg)](http://i.imgur.com/HnHLiv7.jpg)
[![Tablet UAV night](http://i.imgur.com/ehvx1tFm.jpg)](http://i.imgur.com/ehvx1tF.jpg)
[![3D models](http://i.imgur.com/Niynrvmm.jpg)](http://i.imgur.com/Niynrvm.jpg)
[![Vehicle FBCB2](http://i.imgur.com/bjarZTqm.jpg)](http://i.imgur.com/bjarZTq.jpg)
[![TAD small](http://i.imgur.com/ngtjm2Dm.jpg)](http://i.imgur.com/ngtjm2D.jpg)
[![TAD large night](http://i.imgur.com/OVyYrkpm.jpg)](http://i.imgur.com/OVyYrkp.jpg)
[![Android small](http://i.imgur.com/0lOIuvem.jpg)](http://i.imgur.com/0lOIuve.jpg)
[![Android large night](http://i.imgur.com/aaPccktm.jpg)](http://i.imgur.com/aaPcckt.jpg)
[![MicroDAGR small](http://i.imgur.com/ZI6XZznm.jpg)](http://i.imgur.com/ZI6XZzn.jpg)
[![MicroDAGR large](http://i.imgur.com/aE3zcxjm.jpg)](http://i.imgur.com/aE3zcxj.jpg)
[![cTab 1.0 release overview](http://img.youtube.com/vi/2fFSOej_GPk/0.jpg)](http://youtu.be/2fFSOej_GPk)

Download / Links
----------------
New version :
* [cTab+ on Steam Workshop](https://steamcommunity.com/workshop/filedetails/?id=2262006564)
* [cTab IRL on Steam Workshop](https://steamcommunity.com/workshop/filedetails/?id=2262009445)

Previous versions from Riouken
* [Armaholic](http://www.armaholic.com/page.php?id=22992)
* [ArmA World](http://armaworld.de/threads/156-cTab-Commander-s-Tablet)
* [withSix](http://play.withsix.com/Arma-3/mods/4KfaixFS4xGnygAVF72WTA)
* [BI Forum](http://forums.bistudio.com/showthread.php?166488)
* [GitHub](https://github.com/Riouken/cTab)

How to contribute
-----------------
Code contributions are welcome.

- To build and test modifications of @cTab you can use [HEMTT](https://hemtt.dev/index.html)
