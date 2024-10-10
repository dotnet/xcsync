namespace xcsync;
#pragma warning disable IO0006 // Replace Path class with IFileSystem.Path for improved testability
public interface IFilesystemEventFilter
{
    bool ProcessEvent(string path); // You will have to list each event
    bool ProcessRenameEvent(string origin, string destination);
}

public class ExtensionFilter (IEnumerable<string> extensionsToMonitor) : IFilesystemEventFilter
{
    readonly HashSet<string> _extensionsToMonitor = new(extensionsToMonitor, StringComparer.OrdinalIgnoreCase);

	public bool ProcessEvent(string path)
    {
        var extension = Path.GetExtension(path);
        return _extensionsToMonitor.Contains(extension);
    }

    public bool ProcessRenameEvent(string origin, string destination)
    {
		var originExtension = Path.GetExtension(origin);
		var destinationExtension = Path.GetExtension(destination);
        return _extensionsToMonitor.Contains(originExtension) || _extensionsToMonitor.Contains(destinationExtension);
    }

	public string GetExtensionsToMonitorAsString () => string.Join (", ", _extensionsToMonitor.Select (ext => ext));
}
#pragma warning restore IO0006 // Replace Path class with IFileSystem.Path for improved testability
