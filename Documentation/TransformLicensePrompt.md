添付のVN3ライセンスPDFを読み取り、下記のJSONスキーマに従って出力してください。

# 出力スキーマ

{
  "schemaVersion": 1,
  "productName": "",
  "vendorName": "",
  "scope": "product",
  "sourceDocuments": ["Assets/（このPDFのプロジェクト内パスを記入してください）"],
  "permissions": {
    "personalUse": "",
    "corporateUse": "",
    "commercialUse": "",
    "modification": "",
    "redistribution": "",
    "modifiedRedistribution": "",
    "vrchatUpload": "",
    "publicAvatar": "",
    "pedestalUse": "",
    "videoStreaming": "",
    "socialMediaPosting": "",
    "adultExpression": "",
    "violentExpression": "",
    "politicalReligiousUse": "",
    "nftUse": "",
    "aiTraining": ""
  },
  "requirements": {
    "credit": "",
    "usageReport": "",
    "contactBeforeUse": ""
  },
  "notes": "",
  "lastReviewedAt": "YYYY-MM-DD"
}

# 各フィールドに入れる値

以下のいずれかのみを使ってください。

値           意味
allow        許可されている
deny         禁止されている
conditional  条件付き許可（条件は notes に記載する）
ask          作者への個別確認が必要
unknown      記載があるが判断できない
notMentioned PDF に記載がない

# VN3ライセンス項目とフィールドの対応

PDF上の項目          → JSONフィールド
個人利用             → personalUse
法人利用             → corporateUse
商業利用             → commercialUse
改変                 → modification
再配布               → redistribution
改変後の再配布       → modifiedRedistribution
VRChatアップロード   → vrchatUpload
パブリックアバター   → publicAvatar
台座（Pedestal）利用 → pedestalUse
動画・配信利用       → videoStreaming
SNS投稿              → socialMediaPosting
性的・アダルト表現   → adultExpression
暴力的表現           → violentExpression
政治的・宗教的利用   → politicalReligiousUse
NFT利用              → nftUse
AI学習               → aiTraining
クレジット表記       → requirements.credit
利用報告             → requirements.usageReport
事前連絡             → requirements.contactBeforeUse

# 解釈のルール

- チェック欄が「○」「可」「許可」→ allow
- チェック欄が「×」「不可」「禁止」→ deny
- チェック欄が「△」「条件付き」「要相談」→ conditional または ask
- PDF上で一切触れられていない項目は notMentioned にしてください
- 記載はあるが読み取れない・判断できない場合のみ unknown を使ってください
- 製品名・販売者名がPDFに書かれていれば productName / vendorName に入れてください

# notes フィールドの書き方（重要）

- conditional または ask にした項目がある場合、その理由・条件・補足を notes フィールドに記載してください。
- **1項目につき必ず1行**で書いてください。複数の項目を並べるときは、必ず各項目の前に改行を入れてください。
- JSON 文字列内での改行は `\n` で表現します。
- 各行の形式: `【フィールド名】説明文`

正しい例（各項目が改行で区切られている）:
```
"notes": "【commercialUse】個人による商用利用は許可されるが、クレジット表記が必須。\n【adultExpression】成人向けコンテンツである旨の棲み分けを行うこと。\n【credit】商用利用時はクレジット表記必須、通常利用時は任意。"
```

誤った例（改行なしで連結している）:
```
"notes": "【commercialUse】説明...【adultExpression】説明...【credit】説明..."
```

# 出力の手順

1. まず、判断に迷った項目があれば箇条書きで一言コメントしてください（1項目1行）
2. その後、有効なJSONブロックを1つだけ出力してください
3. JSONブロック外に説明文を混ぜないでください