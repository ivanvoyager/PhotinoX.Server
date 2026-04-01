using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.FileProviders;

namespace Photino.NET.Server;

/// <summary>
/// The PhotinoServer class enables users to host their web projects in
/// a static, local file server to prevent CORS and other issues.
/// </summary>
public class PhotinoServer
{
    public const string DefaultWebRoot = "wwwroot";
    public const string DefaultSpaIndex = "index.html";
    public const string EmbeddedResourcePrefix = "Resources";

    public static WebApplication CreateStaticFileServer(string[] args, out string baseUrl) =>
        CreateStaticFileServer(args, startPort: 8000, portRange: 100, webRootFolder: DefaultWebRoot, out baseUrl);

    public static WebApplication CreateStaticFileServer(string[] args, int startPort, int portRange, string webRootFolder, out string baseUrl) =>
        CreateStaticFileServer(args, startPort, portRange, webRootFolder, enableSpaFallback: false, spaIndexFile: DefaultSpaIndex, out baseUrl);

    public static WebApplication CreateStaticFileServer(
        string[] args,
        int startPort,
        int portRange,
        string webRootFolder,
        bool enableSpaFallback,
        string spaIndexFile,
        out string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootFolder);

        // Ensure web root exists on disk
        Directory.CreateDirectory(webRootFolder);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            WebRootPath = webRootFolder
        });

        // Try to read files from embedded resources: Resources/{webRootFolder}
        var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
        var manifestEmbeddedFileProvider = new ManifestEmbeddedFileProvider(assembly, $"{EmbeddedResourcePrefix}/{webRootFolder.TrimStart('/', '\\')}");

        var physicalFileProvider = builder.Environment.WebRootFileProvider;

        // Prefer disk; fallback to embedded resources
        CompositeFileProvider compositeWebProvider = new(physicalFileProvider, manifestEmbeddedFileProvider);

        builder.Environment.WebRootFileProvider = compositeWebProvider;

        // Pick a free port
        int port = FindFreePort(startPort, portRange);

        baseUrl = $"http://localhost:{port}";
        builder.WebHost.UseUrls(baseUrl);

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Optional SPA fallback: only if enabled AND index exists in composite provider
        if (enableSpaFallback)
        {
            spaIndexFile = string.IsNullOrWhiteSpace(spaIndexFile) ? DefaultSpaIndex : spaIndexFile.TrimStart('/', '\\');
            if (IndexExists(compositeWebProvider, spaIndexFile))
            {
                app.MapFallbackToFile(spaIndexFile);
            }
        }

        return app;
    }

    private static int FindFreePort(int startPort, int portRange)
    {
        int endPort = startPort + portRange;
        for (int port = startPort; port <= endPort; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (SocketException)
            {
                continue;
            }
        }
        throw new IOException($"No free port in range {startPort}..{endPort}");
    }

    private static bool IndexExists(CompositeFileProvider provider, string spaIndexFile)
    {
        var file = provider.GetFileInfo(spaIndexFile);
        return file is { Exists: true, IsDirectory: false };
    }
}
