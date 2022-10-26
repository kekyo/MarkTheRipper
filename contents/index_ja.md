---
title: MarkTheRipperドキュメント
lang: ja-jp
date: 10/26/2022 22:24:52 +09:00
---

![MarkTheRipper](Images/MarkTheRipper.100.png)

MarkTheRipper - マークダウンで書く事が出来る、静的サイトの高速生成ツール。

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| MarkTheRipper | [![NuGet MarkTheRipper](https://img.shields.io/nuget/v/MarkTheRipper.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper) |
| MarkTheRipper.Core | [![NuGet MarkTheRipper.Core](https://img.shields.io/nuget/v/MarkTheRipper.Core.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper.Core) |
| MarkTheRipper.Engine | [![NuGet MarkTheRipper.Engine](https://img.shields.io/nuget/v/MarkTheRipper.Engine.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper.Engine) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![MarkTheRipper CI build (main)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Amain) | [![MarkTheRipper CI build (develop)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Adevelop) |

----

[English is here](https://github.com/kekyo/MarkTheRipper)

## これは何？

MarkTheRipperは、非常にシンプルかつ高速な静的サイトジェネレータで、コンテンツをマークダウンで書くことができます。
主に想定される用途はブログサイトですが、まるでGitHub Gistで記事を書いているかのように、とにかく複雑な構造やツールの要求、というものを排除しました。

.NET 6.0をインストールしている環境なら、

```bash
dotnet tool install -g MarkTheRipper
```

とするだけでインストールできます。または、.NET Framework 4.71以上に対応した、[ビルド済みのバイナリをダウンロードする事もできます。](https://github.com/kekyo/MarkTheRipper/releases)

* 0.4.0現在、dotnet toolingでのインストールは、正しくないバージョンがインストールされる問題があり、[修正中です。](https://github.com/kekyo/MarkTheRipper/issues/27)

----

## Install develop branch package

```
$ dotnet tool install -g MarkTheRipper --nuget-source http://nuget.kekyo.online:59103/repository/nuget/index.json
```

----

## License

Apache-v2.

----

## 履歴

* 0.4.0:
  * ページングナビゲーションができるようになりました。#20
* 0.3.0:
  * oEmbed を使用できるようになりました。#18
* 0.2.0:
  * キーワード展開がマークダウン文書そのものに適用できるようになりました。#3
* 0.1.0:
  * マークダウン・ファイルに日付のメタデータを自動的に挿入するようにしました。#13
  * 関数呼び出しができるようになりました。#2
  * カテゴリーとタグをマークダウンされたコンテンツ全体から集計する事が出来るようになりました。#14
  * ブラケット文字に対応しました。#6
  * ジェネレータキーワードを使用できるようになりました。#5
  * カテゴリの自動収集が可能になりました。#1
