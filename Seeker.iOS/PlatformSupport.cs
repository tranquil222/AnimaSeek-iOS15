// Copyright (c) 2026 AnimaSeek iOS 15 contributors.
// Modified 2026-11-21: unofficial iOS 15 compatibility, presentation, or fork identity updates.
using System.Runtime.Versioning;

// This is an iPhone app, not a Mac Catalyst target. In particular, iOS 26's
// continued-processing API has no Catalyst implementation.
[assembly: UnsupportedOSPlatform("maccatalyst")]
