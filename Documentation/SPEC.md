# Avatar License Manager Spec

Date: 2026-05-09

## Purpose

Avatar License Manager is a Unity Editor extension that analyzes a selected VRChat avatar Prefab or Scene object, identifies the avatar-related asset products it depends on, reads or creates machine-readable license summaries for those products, and visualizes the resulting permissions and restrictions for the completed avatar.

The tool is a license confirmation aid. It does not provide legal advice and does not guarantee that a use case is allowed.

## Target Users And Use Cases

- VRChat avatar users who combine avatars, outfits, hair, accessories, shaders, tools, and other BOOTH-style assets.
- Users who want to check whether a completed avatar can be used for personal use, commercial activity, videos, streaming, public avatar upload, redistribution, AI training, or other common use cases.
- Users who want to keep a per-project license record that can be reviewed again after changing outfits or accessories.
- Future users who install the tool into avatar Unity projects through VCC / VPM from a GitHub-hosted package.

## Supported Unity Version

- Unity 2022.3.22f1 is the baseline.
- The package metadata declares Unity 2022.3 compatibility.

## Supported VRChat SDK

- Avatar SDK 3 projects are the primary target.
- The first implementation should be Editor-only.
- Runtime avatar code is not required for the initial scope.
- If Runtime code is introduced later, it must avoid world-only APIs, UdonSharp, `VRC.Udon`, file I/O, networking, and thread processing.

## Tool Name

- Product name: `Avatar License Manager`
- Suggested menu path: `Tools > StarSideUp > Avatar License Manager`
- Suggested folder root: `Assets/StarSideUp/AvatarLicenseManager`
- Suggested package name: `com.starsideup.avatar-license-manager`

## Functional Requirements

- Let the user select an avatar Prefab asset or Scene avatar root.
- Collect dependencies used by that avatar, including Prefabs, FBX/model assets, Materials, Textures, AnimationClips, AnimatorControllers, Expression Menus, ScriptableObjects, and relevant nested Prefabs.
- Group collected assets into asset product candidates.
- Treat `Assets/<VendorName>/<ProductName>` as the default product-root pattern, but allow user correction.
- Never assume that all products under the same vendor/shop folder share the same license.  
- Search for machine-readable license summary files near each product.
- Prefer product-specific license summaries over vendor/shop-level fallback summaries.
- Allow manual correction of product root, product name, vendor name, source license document, and license item values.
- Save manual corrections in a machine-readable JSON file so future analyses can reuse them.
- Show the source of each license value: product JSON, shop fallback JSON, PDF/source document, or manual user entry.
- Aggregate all detected product licenses into an avatar-wide usage summary.
- Mark any missing, unknown, unconfirmed, or ambiguous result as requiring user review.
- Export an avatar license report as Markdown and later JSON.

## Non Goals

- Do not provide legal advice or a final legal guarantee.
- Do not automatically fetch BOOTH pages, purchase histories, or remote license files.
- Do not require internet access.
- Do not OCR image-based PDFs in the first implementation.
- Do not attempt full PDF text extraction in the first implementation.
- Do not modify avatar Prefabs, Materials, AnimatorControllers, or other user assets during analysis.
- Do not apply a whole shop/vendor license as confirmed for every product unless the user explicitly accepts that fallback for a product.

## Folder Structure

- Tool root: `Assets/StarSideUp/AvatarLicenseManager`
- Editor code: `Assets/StarSideUp/AvatarLicenseManager/Editor`
- Runtime code: `Assets/StarSideUp/AvatarLicenseManager/Runtime`
- Generated reports or caches: `Assets/StarSideUp/AvatarLicenseManager/Generated`
- Documentation: `Assets/StarSideUp/AvatarLicenseManager/Documentation`
- Samples: `Assets/StarSideUp/AvatarLicenseManager/Samples`

## Editor And Runtime Responsibilities

- Editor code owns avatar dependency scanning, product grouping, license summary discovery, manual correction UI, report generation, and asset database interaction.
- Runtime code is not required for the initial release.
- Any future Runtime assembly must not reference UnityEditor and must remain safe for VRChat avatar projects.

## Dependencies

- Required: Unity Editor APIs.
- Expected project context: VRChat Avatars SDK 3.
- Optional: Modular Avatar and NDMF may be present but should not be required for the initial license manager.
- Avoid new third-party dependencies for the first implementation.
- PDF files are treated as source documents in the first implementation, not parsed as authoritative structured data.

## Product And License Discovery

### Product Root Model

The tool should model licenses at an asset product level, not at a vendor/shop level.

Default folder convention:

```text
Assets/
  VendorName/
    ProductName/
      Prefabs/
      Materials/
      License/
      avatar-license.json
```

For an asset path such as `Assets/StarSideUp/ExampleOutfit/Prefabs/Outfit.prefab`:

- Vendor candidate: `StarSideUp`
- Product root candidate: `Assets/StarSideUp/ExampleOutfit`
- Product candidate: `ExampleOutfit`

This convention is only a candidate. Users must be able to correct product roots because imported assets may use different folder structures.

### License File Search Priority

For a product root under `Assets/<VendorName>/<ProductName>`, search in this order:

1. Product-root `avatar-license.json`
2. Product-root `license.json`
3. Product-root `vn3-license.json`
4. Product-root license document candidates, including folders named `License`, `Licenses`, `VN3`, `Terms`, `規約`, `利用規約`, or similar
5. Vendor-root `avatar-license.json`
6. Vendor-root `license.json`
7. Vendor-root `vn3-license.json`
8. Vendor-root license document candidates
9. No license found

Product-root files always override vendor-root fallback files.

Vendor-root license summaries may be used as fallback candidates, but the UI must clearly show that a shop-level fallback is being applied. The first implementation should treat fallback usage as requiring user confirmation before it is considered reviewed.

## Machine-Readable License Summary

The primary structured file should be named `avatar-license.json`.

Draft schema:

```json
{
  "schemaVersion": 1,
  "productName": "Example Outfit",
  "vendorName": "Example Shop",
  "scope": "product",
  "sourceDocuments": [
    "Assets/ExampleShop/ExampleOutfit/License/vn3-license.pdf"
  ],
  "permissions": {
    "personalUse": "allow",
    "corporateUse": "unknown",
    "commercialUse": "deny",
    "modification": "allow",
    "redistribution": "deny",
    "modifiedRedistribution": "deny",
    "vrchatUpload": "allow",
    "publicAvatar": "deny",
    "pedestalUse": "unknown",
    "videoStreaming": "allow",
    "socialMediaPosting": "allow",
    "adultExpression": "conditional",
    "violentExpression": "conditional",
    "politicalReligiousUse": "deny",
    "nftUse": "deny",
    "aiTraining": "ask"
  },
  "requirements": {
    "credit": "required",
    "usageReport": "notMentioned",
    "contactBeforeUse": "ask"
  },
  "notes": "Manually summarized from the source PDF.",
  "lastReviewedAt": "2026-05-09"
}
```

### Permission Values

- `allow`: Allowed.
- `deny`: Prohibited.
- `conditional`: Allowed only under stated conditions.
- `ask`: The rights holder must be contacted individually.
- `unknown`: The tool or user cannot determine the value.
- `notMentioned`: The source document does not mention this item.

Suggested aggregation severity:

```text
deny > ask > conditional > unknown > notMentioned > allow
```

### Requirement Values

- `required`: Required.
- `notRequired`: Not required.
- `conditional`: Required only under stated conditions.
- `ask`: Contact the rights holder.
- `unknown`: Unknown.
- `notMentioned`: Not mentioned.

Credit attribution must be tracked as a requirement, not as a permission.

## UI Requirements

- Provide an EditorWindow.
- Show the selected avatar target at the top.
- Provide an Analyze button.
- Show product candidates in a table or list.
- For each product, show product name, vendor name, product root, source license file, license status, confidence, and review state.
- Allow editing a product's license values from the UI.
- Show whether the current license came from product JSON, vendor fallback JSON, source document candidate, or manual input.
- Show an avatar-wide summary grouped into Allowed, Prohibited, Conditional, Ask Rights Holder, Unknown, and Not Mentioned.
- Keep wording clear that this is an aid and final confirmation remains the user's responsibility.

## Display Modes

The tool should support two display modes in a future UI design:

- Simple mode: show the most common user-facing decisions, such as personal use, commercial use, modification, redistribution, public avatar use, video/streaming use, adult expression, AI training, and credit requirement.
- Detailed mode: show all tracked VN3-style permissions and requirements, including corporate use, modified redistribution, pedestal use, social media posting, violent expression, political/religious use, NFT use, usage report, contact-before-use, source document paths, confidence, and review state.

Simple mode should be the default for non-expert users. Detailed mode should preserve all raw values and evidence so advanced users can audit the result without losing information.

## Future Scope

### PDF To JSON Conversion

PDF license files should remain supported as source documents. A future version may add a conversion workflow that helps users create `avatar-license.json` from PDF licenses.

Possible stages:

1. Extract selectable PDF text when available.
2. Detect VN3-style labels and map them to draft permission and requirement values.
3. Show the extracted text and proposed JSON side by side.
4. Require user confirmation before saving any generated JSON.
5. Mark generated values with provenance such as `pdfExtracted`, `userConfirmed`, or `manualOverride`.

Image-only PDFs and difficult Japanese font embeddings should not block the core workflow. OCR can be considered later, but should be optional because it adds dependency and accuracy risk.

### License Freshness Check

Checking whether a license source is the latest version is a low-priority future feature.

Possible approaches:

- Store `lastReviewedAt` and show stale-review warnings after a configurable period.
- Store optional source URLs for product pages or license pages.
- Let users manually mark a license summary as reviewed.
- Avoid automatic web access in the initial version.
- If automatic checking is added later, make it opt-in and clearly separate "source changed" from "license meaning changed".

Freshness checks must never silently change permission values.

## Generated Assets And Regeneration

- User-authored product license summaries should live next to the relevant product when possible.
- Tool-generated reports and caches should live under `Generated`.
- Generated files must not overwrite user-edited license summaries without confirmation.
- Report generation should be repeatable.

## Distribution And GitHub Management

- This folder is intended to be isolated as a standalone GitHub repository.
- Repository root candidate: `Assets/StarSideUp/AvatarLicenseManager`.
- The folder includes a `package.json` with package name `com.starsideup.avatar-license-manager`.
- Future VCC / VPM distribution should publish versioned package releases from the GitHub repository.
- The first repository should keep Unity `.meta` files under version control.
- The wider Unity project, `Library`, `Temp`, `Logs`, `UserSettings`, unrelated `Assets` folders, and unrelated `ProjectSettings` should remain outside the package repository.

Open design note:

- VCC / VPM packages are normally installed as Unity packages under `Packages`.
- This development project keeps product assets under `Assets/StarSideUp/AvatarLicenseManager` to follow the local project rule.
- Before public VPM release, confirm whether the final package layout should remain asset-folder based or move package contents to a UPM-style repository root.

## Verification Plan

- Confirm Unity Console has no compile errors after adding or changing Editor code.
- Confirm the menu appears at `Tools > StarSideUp > Avatar License Manager`.
- Confirm avatar analysis does not modify the target avatar or imported product assets.
- Confirm product-specific `avatar-license.json` overrides vendor-level fallback JSON.
- Confirm vendor-level fallback is visibly marked as fallback and not silently treated as confirmed.
- Confirm unknown or missing licenses produce an avatar-wide review-needed status.
- Confirm report export can be run repeatedly.

## Open Questions

- Should `avatar-license.json` be created inside third-party imported product folders, or should user corrections be stored in a central database to avoid modifying imported assets?
- Should vendor-level fallback require explicit per-product confirmation every time, or only once per product?
- Should the initial implementation include a JSON schema file for external validation?
- Which VN3 items should be shown by default, and which should be hidden under advanced settings?
- What should the exact Simple mode item set be for Japanese VRChat users?
- Should PDF-to-JSON conversion live inside Unity, or should it be a separate helper tool used before importing JSON into Unity?
