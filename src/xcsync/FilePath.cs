// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Globalization;

namespace xcsync;

[System.Diagnostics.CodeAnalysis.SuppressMessage ("System.IO.Abstractions", "IO0006:Replace Path class with IFileSystem.Path for improved testability", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage ("System.IO.Abstractions", "IO0003:Replace Directory class with IFileSystem.Directory for improved testability", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage ("System.IO.Abstractions", "IO0002:Replace File class with IFileSystem.File for improved testability", Justification = "<Pending>")]

[TypeConverter (typeof (FilePathTypeConverter))]
public readonly struct FilePath (string? path) : IComparable<FilePath>, IComparable, IEquatable<FilePath> {
	class FilePathTypeConverter : TypeConverter {
		public override bool CanConvertFrom (ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof (string) || base.CanConvertFrom (context, sourceType);
		}

		public override object? ConvertFrom (ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value == null)
				return Empty;

			return value is string strValue ? new FilePath (strValue) : base.ConvertFrom (context, culture, value);
		}
	}

	public static readonly FilePath Empty = new ();

	readonly string? path = path;

	public bool IsNull => path == null;

	public string? FullPath {
		get {
			if (string.IsNullOrEmpty (path))
				return null;

			var fullPath = Path.GetFullPath (path);

			if (IsLink (fullPath)) {
				FileSystemInfo? targetInfo = Directory.ResolveLinkTarget (fullPath, returnFinalTarget: true);
				if (targetInfo != null)
					fullPath = targetInfo.FullName;
			}

			if (fullPath is null || fullPath.Length == 0)
				return null;

			if (fullPath [^1] == Path.DirectorySeparatorChar)
				return fullPath.TrimEnd (Path.DirectorySeparatorChar);

			if (fullPath [^1] == Path.AltDirectorySeparatorChar)
				return fullPath.TrimEnd (Path.AltDirectorySeparatorChar);

			return fullPath;
		}
	}

	public bool DirectoryExists => path != null && Directory.Exists (path);

	public bool FileExists => path != null && File.Exists (path);

	public FilePath ParentDirectory => new FilePath (Path.GetDirectoryName (path));

	public string? Extension => path != null ? Path.GetExtension (path) : null;

	public string? Name => path != null ? Path.GetFileName (path) : null;

	public string? NameWithoutExtension => path != null ? Path.GetFileNameWithoutExtension (path) : null;

	public DirectoryInfo CreateDirectory () => Directory.CreateDirectory (FullPath ?? ".");

	public IEnumerable<FilePath> EnumerateDirectories (string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		if (path == null)
			yield break;

		foreach (FilePath filePath in Directory.EnumerateDirectories (path, searchPattern, searchOption))
			yield return filePath;
	}

	public IEnumerable<FilePath> EnumerateFiles (string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		if (path == null)
			yield break;

		foreach (FilePath filePath in Directory.EnumerateFiles (path, searchPattern, searchOption))
			yield return filePath;
	}

	public FilePath ChangeExtension (string extension) => Path.ChangeExtension (path, extension);

	public FilePath Combine (params string [] paths) => new FilePath (Path.Combine (path ?? string.Empty, Path.Combine (paths)));

	public static FilePath Build (params string [] paths) => Empty.Combine (paths);

	public int CompareTo (FilePath other) => string.Compare (FullPath, other.FullPath, StringComparison.Ordinal);

	public int CompareTo (object? obj) => obj is FilePath filePath ? CompareTo (filePath) : -1;

	public bool Equals (FilePath other) => string.Equals (FullPath, other.FullPath, StringComparison.Ordinal);

	public override bool Equals (object? obj) => obj is FilePath filePath && Equals (filePath);

	public override int GetHashCode ()
	{
		var fp = FullPath;
		return fp == null ? 0 : fp.GetHashCode ();
	}

	public override string? ToString () => path;

	public static implicit operator FilePath (string? path) => new FilePath (path);

	public static implicit operator string (FilePath path) => string.IsNullOrEmpty (path.path) ? "." : path.path!;

	public static bool operator == (FilePath a, FilePath b) => a.Equals (b);

	public static bool operator != (FilePath a, FilePath b) => !a.Equals (b);

	public static FilePath GetTempPath () => Path.GetTempPath ();

	public FilePath GetTempFileName (string? extension = null)
	{
		if (!string.IsNullOrWhiteSpace (extension)) {
			if (extension! [0] == '.')
				extension = extension [1..];
		}

		if (string.IsNullOrWhiteSpace (extension))
			extension = "tmp";

		return Combine (Guid.NewGuid ().ToString ("N") + "." + extension);
	}

	public TemporaryFileStream GetTempFileStream (string? extension = null)
	{
		var tempFileName = GetTempFileName (extension);
		tempFileName.ParentDirectory.CreateDirectory ();
		return new TemporaryFileStream (tempFileName);
	}

	public class TemporaryFileStream (FilePath path) : FileStream (path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read) {
		public FilePath FileName { get; } = path;
	}

	static bool IsLink (string path)
	{
		if (path == null)
			return false;

		FileAttributes attributes = File.GetAttributes (path);
		return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
	}
}
