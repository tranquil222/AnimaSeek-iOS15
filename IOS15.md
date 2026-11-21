# Unofficial iOS 15 backport

This is a modified, GPL-3.0 fork of [AnimaSeek](https://github.com/alexandreteles/AnimaSeek), targeting iOS 15.0 and later on ARM64 iPhones. It is intended for installation through TrollStore on an already supported device. It does not install a jailbreak or change iOS.

## What changes

- Older iOS versions receive compatible loading/empty/error views, button styles, rounded corners, appearance callbacks, and haptics.
- API availability warnings are build errors. Newer APIs remain behind operating-system checks.
- Before iOS 26, keep the app open while downloading. Switching away or locking the phone gives transfers only the existing short background grace period, after which they pause and checkpoint. Eligible downloads resume when the app returns. Uploads depend on peers requesting them again. This backport does not promise unrestricted background operation.
- The About screen links to this fork's corresponding source. Upstream notices and bundled licenses remain included.

## Build and install

Public preview downloads, including the IPA, matching source archive, license files, and checksums, are hosted on [this fork's Releases page](https://github.com/tranquil222/AnimaSeek-iOS15/releases). These are unofficial community builds, not upstream releases. Open the IPA in TrollStore. When updating this backport, install over the existing app to retain its container; do not uninstall first. The bundle identifier remains `com.animaseek.app`, so this replaces any other AnimaSeek installed with that identifier.

Run **Build iOS 15 backport** (`.github/workflows/ios15.yml`) manually in this public fork's GitHub Actions tab. It runs portable and protocol regression tests, compiles a strict mock simulator build, then builds the real ARM64 Release app without Apple code signing. The toolchain is .NET SDK 10.0.302, the iOS workload, and Xcode 26.6 on the standard `macos-26` runner. This workflow does not publish a release or use an Apple developer account.

Download the **AnimaSeek-iOS15-TrollStore** artifact and extract it. It contains `AnimaSeek-iOS15.ipa`, its SHA-256 checksum, and the source commit. Transfer the IPA to the phone, share it to TrollStore, and install it. The GitHub artifact expires after three days; save a local copy. Unlike upstream releases, this artifact has no SBOM or signed release attestation.

Before packaging, `scripts/verify-ios15-bundle.py` checks the bundle identifier and deployment floor, every included Mach-O binary's deployment floor, and that binaries are not App Store encrypted. An installable IPA still needs a physical-device check; a successful build does not establish runtime compatibility.

## Validation status

The [hosted build](https://github.com/tranquil222/AnimaSeek-iOS15/actions/runs/44900598116) passed on 2026-11-21 for source commit `1e5da24b4e8709597652697df4d4aaf1ca9462e4`:

- 628 portable regression tests and 21 protocol AOT regression tests passed, both locally and on GitHub.
- The strict `Debug Mock` simulator build and real ARM64 Release build passed with full trimming and no interpreter. Existing compiler warnings remain; these were not warning-free builds.
- The IPA's bundle and native executable both declare a minimum of iOS 15.0; the executable is unencrypted. The verification was repeated after downloading the artifact, and its SHA-256 matched.
- `AnimaSeek-iOS15.ipa`: 17,016,466 bytes, SHA-256 `db87d7cc88de487982b0bdafdc6a130c04e58d6614a87df074d4608612d06fc7`. The bundle retains upstream's checked-in display version `3.7.4` and build `136`; identify this backport by source commit and checksum.

On 2026-11-21, the device owner confirmed that installation and launch succeeded on an iPhone 7 running iOS 15.8.4, and that a file downloaded and played successfully. This is a user-reported smoke check, not an independently observed comprehensive device test. Background expiration/resume and the complete appearance/accessibility matrix remain unverified.

The owner reported that file/folder action menus and the tab bar were too transparent over lists. Per-app Reduce Transparency did not resolve it. The 3.7.5 / 137 update replaces the primary file/folder menus with solid, scrollable sheets and gives the tab/navigation bars opaque standard and scroll-edge appearances. It also retains progress indicators across nonanimated transfer-list updates to address intermittent wiping/flashing reported while downloads continued. This update labels the app AnimaSeek 15 and uses protocol minor identifier 20261501 to distinguish the public fork from upstream AnimaSeek. Its physical-device behavior remains to be checked.

Mock-only previews are available with `ANIMASEEK_UI_MENU=file` or `folder`; they never contact the live network. `scripts/capture-menu-previews.py` captures the component in light/dark and accessibility-extra-extra-extra-large on a 375 × 667-point simulator, plus the search screen. These previews validate layout on a current simulator, not iOS 15 runtime behavior or actual action routing.

The SDK 26 build sets `UIDesignRequiresCompatibility` to retain classic system chrome on newer iOS releases. This complements the explicit opaque backgrounds; it is not needed by iOS 15. Apple documents this flag as temporary and ignores it for apps built with SDK 27 or later, so a future toolchain upgrade must review the appearance again. Action menus open at the large sheet detent and remain scrollable for long labels and accessibility text sizes.

On a device, check cold launch, sign-in with your own Soulseek account, search, a permitted test download, background pause/foreground resume, light/dark appearance, and larger text. Automated checks use mocks and never sign in to the live Soulseek network.

For a limited mobile-data plan, disable AnimaSeek under **Settings → Cellular** and download over Wi-Fi. Only select folders you intend to share with other Soulseek users.
