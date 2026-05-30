# Avatar License Manager VPM Distribution Handoff

Date: 2026-05-30

## Completed

- Created a public package listing repository: `https://github.com/Favnian/AvatarLicenseManager-VPM`
- Prepared listing source for VCC URL: `https://favnian.github.io/AvatarLicenseManager-VPM/index.json`
- Configured the listing to reference package repository `Favnian/AvatarLicenseManager`.
- Added a package release workflow draft at `.github/workflows/release.yml`.
- Updated `package.json` with `license`, `author.url`, and `vpmDependencies`.

## Recommendation

- Keep `AvatarLicenseManager` as the package/distribution repository.
- Use `AvatarLicenseManager-VPM` only as the VPM package listing repository.
- Do not rename the existing repository unless a separate private development mirror becomes necessary.

## Verification

- Confirmed `package.json` parses as JSON.
- Confirmed `source.json` parses as JSON.
- Confirmed GitHub CLI access works with the `Favnian` account when run with external access.

## Not Yet Verified

- GitHub Pages deployment has not been verified.
- The package release workflow has not been run.
- VCC installation from the listing URL has not been verified.

## Remaining Tasks

- Add a real `author.email` to `package.json` before publishing if VPM validation requires it.
- Make `Favnian/AvatarLicenseManager` public before the listing is expected to resolve package releases.
- Publish the first release through the `Build Release` workflow.
- Enable GitHub Pages with GitHub Actions for `Favnian/AvatarLicenseManager-VPM`.
- Rebuild the listing after the first package release.
