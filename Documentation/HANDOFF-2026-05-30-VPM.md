# Avatar License Manager VPM Distribution Handoff

Date: 2026-05-30

## Completed

- Created a public package listing repository: `https://github.com/Favnian/AvatarLicenseManager-VPM`
- Prepared listing source for VCC URL: `https://favnian.github.io/AvatarLicenseManager-VPM/index.json`
- Configured the listing to reference package repository `Favnian/AvatarLicenseManager`.
- Added a package release workflow draft at `.github/workflows/release.yml`.
- Updated `package.json` with `license`, `author.email`, `author.url`, and `vpmDependencies`.

## Recommendation

- Keep `AvatarLicenseManager` as the package/distribution repository.
- Use `AvatarLicenseManager-VPM` only as the VPM package listing repository.
- Do not rename the existing repository unless a separate private development mirror becomes necessary.

## Verification

- Confirmed `package.json` parses as JSON.
- Confirmed `source.json` parses as JSON.
- Confirmed GitHub CLI access works with the `Favnian` account when run with external access.
- Confirmed `Favnian/AvatarLicenseManager` is public.
- Published GitHub Release `0.1.0`: `https://github.com/Favnian/AvatarLicenseManager/releases/tag/0.1.0`
- Confirmed release assets include package zip, `.unitypackage`, and `package.json`.
- Rebuilt and deployed the VPM listing through GitHub Pages.
- Confirmed VPM index is reachable: `https://favnian.github.io/AvatarLicenseManager-VPM/index.json`

## Not Yet Verified

- VCC installation from the listing URL has not been verified.

## Remaining Tasks

- Replace the GitHub noreply `author.email` in `package.json` with a project contact email later if desired.
- Add `https://favnian.github.io/AvatarLicenseManager-VPM/index.json` to VCC and verify installation into a clean project.
- Consider changing the next public package version to a preview-style version such as `0.1.1-preview` or documenting that `0.1.0` is an early preview.
