# PhotinoX.Server

[![NuGet Version](https://img.shields.io/nuget/v/PhotinoX.Server.svg)](https://www.nuget.org/packages/PhotinoX.Server)
[![Build](https://github.com/ivanvoyager/PhotinoX.Server/actions/workflows/build.yml/badge.svg)](https://github.com/ivanvoyager/PhotinoX.Server/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/ivanvoyager/PhotinoX.Server?label=license)](https://github.com/ivanvoyager/PhotinoX.Server/blob/master/LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PhotinoX.Server.svg)](https://www.nuget.org/packages/PhotinoX.Server)

Optional static-file server (Kestrel) for **PhotinoX** apps.  
Use it to serve local `wwwroot` and ESM modules without browser restrictions (CORS, file://).  
Useful when JavaScript modules cannot be loaded directly from disk.

> `PhotinoX.Server` is an independent fork of [tryphotino/photino.NET.Server](https://github.com/tryphotino/photino.NET.Server) under the Apache‑2.0 license and is **not affiliated** with the original project or organization.

---

## Install
```bash
dotnet add package PhotinoX.Server
```
> Targets **net8.0; net9.0; net10.0**.

## Samples

`Photino.HelloPhotino.StaticFileServer` example is available in:
- https://github.com/ivanvoyager/PhotinoX.Samples

## Why this server?

Browsers block many operations from `file://` (CORS) and refuse to load ESM modules without proper HTTP/MIME. **PhotinoX.Server** runs a minimal Kestrel host so your app serves `wwwroot` and modules over `http://127.0.0.1:<port>`, avoiding those restrictions.

## Notes

- Minimal defaults (CORS *, static files, default documents).
- No MVC/SignalR — focused on local dev / packaged desktop apps.
- Works on **Windows, macOS, Linux** as long as `PhotinoX.Native` supports the platform.

## Build from source

```bash
dotnet restore Photino.NET.Server\PhotinoX.Server.csproj
dotnet build   Photino.NET.Server\PhotinoX.Server.csproj -c Release
dotnet pack    Photino.NET.Server\PhotinoX.Server.csproj -c Release -o artifacts
```
> CI: see `.github/workflows/build.yml` (build + pack + upload `.nupkg`/`.snupkg`).

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## License

PhotinoX.Server is licensed under **Apache‑2.0**.