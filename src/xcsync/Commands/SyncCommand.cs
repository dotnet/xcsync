namespace xcsync.Commands;

public class SyncCommand {
	public static void Execute (DirectoryInfo project, DirectoryInfo target, bool force)
	{
		Console.WriteLine ($"Syncing files from project '{project}' to target '{target}'");
		// Implement logic here
	}
}
