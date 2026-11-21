using System.Runtime.Versioning;

// This is an iPhone app, not a Mac Catalyst target. In particular, iOS 26's
// continued-processing API has no Catalyst implementation.
[assembly: UnsupportedOSPlatform("maccatalyst")]
