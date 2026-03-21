using System.Net.NetworkInformation;
using Microsoft.Extensions.FileProviders;

namespace Photino.NET.Server;

/// <summary>
/// The PhotinoServer class enables users to host their web projects in
/// a static, local file server to prevent CORS and other issues.
/// </summary>
public class PhotinoServer
{
    public static WebApplication CreateStaticFileServer(string[] args, out string baseUrl) =>
        CreateStaticFileServer(args, startPort: 8000, portRange: 100, webRootFolder: "wwwroot", out baseUrl);

    public static WebApplication CreateStaticFileServer(string[] args, int startPort, int portRange, string webRootFolder, out string baseUrl) =>
        CreateStaticFileServer(args, startPort, portRange, webRootFolder, enableSpaFallback: false, spaIndexFile: "index.html", out baseUrl);

    public static WebApplication CreateStaticFileServer(
        string[] args,
        int startPort,
        int portRange,
        string webRootFolder,
        bool enableSpaFallback,
        string spaIndexFile,
        out string baseUrl)
    {
        // Ensure web root exists on disk
        Directory.CreateDirectory(webRootFolder);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            WebRootPath = webRootFolder
        });

        // Try to read files from embedded resources: Resources/{webRootFolder}
        var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
        var manifestEmbeddedFileProvider = new ManifestEmbeddedFileProvider(assembly, $"Resources/{webRootFolder.TrimStart('/', '\\')}");

        var physicalFileProvider = builder.Environment.WebRootFileProvider;

        // Prefer disk; fallback to embedded resources
        CompositeFileProvider compositeWebProvider =
            new(physicalFileProvider, manifestEmbeddedFileProvider);

        builder.Environment.WebRootFileProvider = compositeWebProvider;

        // Pick a free port
        int port = startPort;
        int endPort = startPort + portRange;

        var ip = IPGlobalProperties.GetIPGlobalProperties();
        while (ip.GetActiveTcpListeners().Any(x => x.Port == port))
        {
            port++;
            if (port > endPort)
                throw new SystemException($"Couldn't find open port within range {startPort}..{endPort}.");
        }

        baseUrl = $"http://localhost:{port}";
        builder.WebHost.UseUrls(baseUrl);

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Optional SPA fallback: only if enabled AND index exists in composite provider
        if (enableSpaFallback)
        {
            spaIndexFile = string.IsNullOrWhiteSpace(spaIndexFile) ? "index.html" : spaIndexFile.TrimStart('/', '\\');
            if (IndexExists(compositeWebProvider, spaIndexFile))
            {
                app.MapFallbackToFile(spaIndexFile);
            }
        }

        return app;
    }

    private static bool IndexExists(IFileProvider provider, string spaIndexFile)
    {
        var file = provider.GetFileInfo(spaIndexFile);
        return file is { Exists: true, IsDirectory: false };
    }
}
