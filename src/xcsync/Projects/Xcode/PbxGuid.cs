// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Xamarin.MacDev;

namespace xcsync.Projects.Xcode;

public readonly struct PbxGuid : IEquatable<PbxGuid> {
	public static readonly PbxGuid Empty = new ();

	readonly Guid guid;

	public static PbxGuid NewGuid ()
	{
		var guidBytes = new byte [12];
		using var rng = RandomNumberGenerator.Create ();
		rng.GetBytes (guidBytes);
		Array.Resize (ref guidBytes, 16);
		return new PbxGuid (guidBytes);
	}

	PbxGuid (byte [] bytes) => guid = new Guid (bytes);

	public PbxGuid (string guid)
	{
		ArgumentNullException.ThrowIfNull (guid, nameof (guid));

		this.guid = guid.Length switch {
			24 => Guid.Parse (guid + "00000000"),
			32 => Guid.Parse (guid),
			_ => throw new ArgumentOutOfRangeException (nameof (guid),
							$"length must be 24 or 32 characters (guid={guid})"),
		};
	}

	public override int GetHashCode () => guid.GetHashCode ();

	public bool Equals (PbxGuid other) => this == other;

	public override bool Equals (object? obj)
	{
		if (obj is not PbxGuid)
			return false;

		return this == (PbxGuid) obj;
	}

	public override string ToString ()
	{
		var str = guid.ToString ("N").ToUpperInvariant ();
		if (str.EndsWith ("00000000", StringComparison.Ordinal))
			str = str [..24];
		return str;
	}

	public static implicit operator PbxGuid (string guid) => new(guid);

	public static implicit operator PString (PbxGuid guid)
	{
		return new PString (guid.ToString ());
	}

	public static explicit operator PbxGuid (PObject pobject)
	{
		return new PbxGuid (((PString) pobject).Value);
	}

	public static bool operator == (PbxGuid left, PbxGuid right) => left.guid == right.guid;

	public static bool operator != (PbxGuid left, PbxGuid right) => left.guid != right.guid;
}