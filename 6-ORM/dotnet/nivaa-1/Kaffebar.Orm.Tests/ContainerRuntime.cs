using System.Diagnostics;
using System.Net.Sockets;
using DotNet.Testcontainers.Configurations;

namespace Kaffebar.Orm.Tests;

/// <summary>
/// Finner en Docker-kompatibel socket, uansett om du bruker Podman, Docker Desktop,
/// Rancher Desktop eller Colima. Testcontainers trenger bare en socket som snakker
/// Docker-API-et, og alle fire har en. De legger den bare på ulike steder.
/// </summary>
/// <remarks>
/// Rekkefølgen:
/// <list type="number">
///   <item><c>DOCKER_HOST</c> er satt: den vinner alltid, vi rører ingenting.</item>
///   <item>Ellers: første kandidat under som faktisk svarer. En socket-fil kan godt bli
///   liggende etter at Docker eller Podman er stoppet, så vi prøver å koble til.</item>
///   <item>Svarer ingen: Testcontainers får prøve sine egne standarder. På Windows
///   betyr det Docker Desktop sin named pipe, som også Rancher Desktop kan tilby.</item>
/// </list>
/// Podman kjører Ryuk (Testcontainers sin opprydder) dårlig, så den skrus av der.
/// Containerne ryddes uansett i <see cref="PostgresFixture.DisposeAsync"/>.
/// </remarks>
internal static class ContainerRuntime
{
    public static (Uri? Endpoint, string Description) Detect()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            if (dockerHost.Contains("podman", StringComparison.OrdinalIgnoreCase))
                TestcontainersSettings.ResourceReaperEnabled = false;
            return (null, $"DOCKER_HOST={dockerHost}");
        }

        if (RunningPodmanMachine() is { } podman)
        {
            TestcontainersSettings.ResourceReaperEnabled = false;
            return podman;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");

        (string Name, string? Path, bool IsPodman)[] sockets =
        [
            ("Podman (Linux)", xdgRuntime is null ? null : Path.Combine(xdgRuntime, "podman", "podman.sock"), true),
            ("Docker Desktop", Path.Combine(home, ".docker", "run", "docker.sock"), false),
            ("Rancher Desktop", Path.Combine(home, ".rd", "docker.sock"), false),
            ("Colima", Path.Combine(home, ".colima", "default", "docker.sock"), false),
            ("Standard socket", "/var/run/docker.sock", false)
        ];

        foreach (var (name, path, isPodman) in sockets)
        {
            if (path is null || !IsListening(path))
                continue;

            if (isPodman)
                TestcontainersSettings.ResourceReaperEnabled = false;

            return (new Uri($"unix://{path}"), $"{name} ({path})");
        }

        return (null, "fant ingen kjent socket som svarer, Testcontainers prøver sine egne standarder");
    }

    /// <summary>
    /// På macOS og Windows kjører Podman i en VM, og socketen ligger et sted bare
    /// <c>podman</c> selv vet om. Vi spør etter maskinen som faktisk kjører (en stoppet
    /// maskin har fortsatt en socket-fil), og så etter socketen eller pipen dens.
    /// </summary>
    private static (Uri, string)? RunningPodmanMachine()
    {
        var machines = Podman("machine", "list", "--format", "{{.Name}}|{{.Running}}");
        var running = machines?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('|'))
            .Where(parts => parts.Length == 2 && parts[1].Equals("true", StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[0].TrimEnd('*'))
            .FirstOrDefault();
        if (running is null)
            return null;

        // Windows: en named pipe. Alt annet: en unix-socket.
        var field = OperatingSystem.IsWindows() ? "{{.ConnectionInfo.PodmanPipe.Path}}" : "{{.ConnectionInfo.PodmanSocket.Path}}";
        var path = Podman("machine", "inspect", running, "--format", field)?.Split('\n')[0].Trim();
        if (string.IsNullOrEmpty(path))
            return null;

        var description = $"Podman machine «{running}» ({path})";
        if (path.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase))
            return (new Uri("npipe://./pipe/" + path[@"\\.\pipe\".Length..]), description);

        return IsListening(path) ? (new Uri($"unix://{path}"), description) : null;
    }

    private static string? Podman(params string[] arguments)
    {
        try
        {
            var info = new ProcessStartInfo("podman") { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in arguments)
                info.ArgumentList.Add(argument);

            using var process = Process.Start(info);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            // Podman er ikke installert. Helt greit, vi prøver de andre.
            return null;
        }
    }

    /// <summary>Finnes socketen, og svarer noen i den andre enden?</summary>
    private static bool IsListening(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(path));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
