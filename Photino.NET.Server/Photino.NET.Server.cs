using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.FileProviders;

namespace Photino.NET.Server;

/// <summary>
/// The PhotinoServer class enables users to host their web projects in
/// a static, local file server to prevent CORS and other issues.
/// </summary>
public static class PhotinoServer
{
    /// <summary>
    /// The default web root folder used by the static file server.
    /// </summary>
    public const string DefaultWebRoot = "wwwroot";

    /// <summary>
    /// The default SPA fallback index file.
    /// </summary>
    public const string DefaultSpaIndex = "index.html";

    /// <summary>
    /// The default embedded resource prefix used when resolving embedded static files.
    /// </summary>
    public const string EmbeddedResourcePrefix = "Resources";

    /// <summary>
    /// Creates a local static file server using the default port range and web root folder.
    /// </summary>
    /// <param name="args">Application command-line arguments passed to the web application builder.</param>
    /// <param name="baseUrl">The selected base URL for the local server.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication CreateStaticFileServer(string[] args, out string baseUrl) =>
        CreateStaticFileServer(args, startPort: 8000, portRange: 100, webRootFolder: DefaultWebRoot, out baseUrl);

    /// <summary>
    /// Creates a local static file server using the specified port range and web root folder.
    /// </summary>
    /// <param name="args">Application command-line arguments passed to the web application builder.</param>
    /// <param name="startPort">The first port to try.</param>
    /// <param name="portRange">The number of ports to scan, starting from <paramref name="startPort"/>.</param>
    /// <param name="webRootFolder">The physical web root folder.</param>
    /// <param name="baseUrl">The selected base URL for the local server.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication CreateStaticFileServer(
        string[] args,
        int startPort,
        int portRange,
        string webRootFolder,
        out string baseUrl) =>
        CreateStaticFileServer(args, startPort, portRange, webRootFolder, enableSpaFallback: false, spaIndexFile: DefaultSpaIndex, out baseUrl);

    /// <summary>
    /// Creates a local static file server using the specified port range, web root folder, and optional SPA fallback.
    /// </summary>
    /// <param name="args">Application command-line arguments passed to the web application builder.</param>
    /// <param name="startPort">The first port to try.</param>
    /// <param name="portRange">The number of ports to scan, starting from <paramref name="startPort"/>.</param>
    /// <param name="webRootFolder">The physical web root folder.</param>
    /// <param name="enableSpaFallback">Whether to map unmatched requests to the SPA index file.</param>
    /// <param name="spaIndexFile">The SPA index file used for fallback routing.</param>
    /// <param name="baseUrl">The selected base URL for the local server.</param>
    /// <returns>The configured web application.</returns>
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

        // Ensure the physical web root exists.
        if (!Directory.Exists(webRootFolder))
        {
            Directory.CreateDirectory(webRootFolder);
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            WebRootPath = webRootFolder
        });

        // Try to read files from embedded resources: Resources/{webRootFolder}.
        var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();

        var embeddedWebRoot = webRootFolder
            .TrimStart('/', '\\')
            .Replace('\\', '/');

        var manifestEmbeddedFileProvider = new ManifestEmbeddedFileProvider(
            assembly,
            $"{EmbeddedResourcePrefix}/{embeddedWebRoot}");

        var physicalFileProvider = builder.Environment.WebRootFileProvider;

        // Prefer disk files and fall back to embedded resources.
        CompositeFileProvider compositeWebProvider = new(physicalFileProvider, manifestEmbeddedFileProvider);

        builder.Environment.WebRootFileProvider = compositeWebProvider;

        // Pick a free local port.
        int port = FindFreePort(startPort, portRange);

        baseUrl = $"http://localhost:{port}";
        builder.WebHost.UseUrls(baseUrl);

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Optional SPA fallback: only if enabled and the index file exists in the composite provider.
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
        ArgumentOutOfRangeException.ThrowIfLessThan(startPort, IPEndPoint.MinPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startPort, IPEndPoint.MaxPort);
        ArgumentOutOfRangeException.ThrowIfLessThan(portRange, 1);

        var endPort = (int)Math.Min(IPEndPoint.MaxPort, (long)startPort + portRange - 1);

        for (int port = startPort; port <= endPort; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return port;
            }
            catch (SocketException)
            {
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
