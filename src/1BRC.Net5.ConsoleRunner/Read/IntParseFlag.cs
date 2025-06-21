using System;

namespace _1BRC.Net9.ConsoleRunner.Read {
	[Flags]
	internal enum IntParseFlag : byte {
		None = 0,
		Signed = 1,
		HasDot = 2
	}
}