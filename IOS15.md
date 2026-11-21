# Unofficial iOS 15 backport

This is a modified, GPL-3.0 fork of [AnimaSeek](https://github.com/alexandreteles/AnimaSeek), targeting iOS 15.0 and later on ARM64 iPhones. It is intended for installation through TrollStore on an already supported device. It does not install a jailbreak or change iOS.

## What changes

- Older iOS versions receive compatible loading/empty/error views, button styles, rounded corners, appearance callbacks, and haptics.
- API availability warnings are build errors. Newer APIs remain behind operating-system checks.
- Before iOS 26, keep the app open while downloading. Switching away or locking the phone gives transfers only the existing short background grace period, after which they pause and checkpoint. Eligible downloads resume when the app returns. Uploads depend on peers requesting them again. This backport does not promise unrestricted background operation.
- The About screen links to this fork's corresponding source. Upstream notices and bundled licenses remain included.

## Build and install

Run **Build iOS 15 backport** (`.github/workflows/ios15.yml`) manually in this public fork's GitHub Actions tab. It runs portable and protocol regression tests, compiles a strict mock simulator build, then builds the real ARM64 Release app without Apple code signing. The toolchain is .NET SDK 10.0.302, the iOS workload, and Xcode 26.6 on the standard `macos-26` runner. This workflow does not publish a release or use an Apple developer account.

Download the **AnimaSeek-iOS15-TrollStore** artifact and extract it. It contains `AnimaSeek-iOS15.ipa`, its SHA-256 checksum, and the source commit. Transfer the IPA to the phone, share it to TrollStore, and install it. The GitHub artifact expires after three days; save a local copy. Unlike upstream releases, this artifact has no SBOM or signed release attestation.

Before packaging, `scripts/verify-ios15-bundle.py` checks the bundle identifier and deployment floor, every included Mach-O binary's deployment floor, and that binaries are not App Store encrypted. An installable IPA still needs a physical-device check; a successful build does not establish runtime compatibility.

## Validation status

The portable regression suite has passed with 628 tests. A separate Linux compile-only check against Microsoft.iOS reference assemblies passed API availability analysis with an iOS 15 floor. These checks do not run UIKit or native AOT code. The hosted Mac build and physical iPhone validation are pending.

On a device, check cold launch, sign-in with your own Soulseek account, search, a permitted test download, background pause/foreground resume, light/dark appearance, and larger text. Automated checks use mocks and never sign in to the live Soulseek network.

For a limited mobile-data plan, disable AnimaSeek under **Settings → Cellular** and download over Wi-Fi. Only select folders you intend to share with other Soulseek users.
