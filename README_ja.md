# MarkTheRipper

![MarkTheRipper](Images/MarkTheRipper.100.png)

MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| MarkTheRipper | [![NuGet MarkTheRipper](https://img.shields.io/nuget/v/MarkTheRipper.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper) |
| MarkTheRipper.Core | [![NuGet MarkTheRipper.Core](https://img.shields.io/nuget/v/MarkTheRipper.Core.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper.Core) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![MarkTheRipper CI build (main)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Amain) | [![MarkTheRipper CI build (develop)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Adevelop) |

----

[English is here](https://github.com/kekyo/MarkTheRipper)

## これは何？

MarkTheRipperは、非常にシンプルかつ高速なスタティックサイトジェネレータで、コンテンツをマークダウンで書くことができます。
主に想定される用途はブログサイトですが、まるでGitHub Gistで記事を書いているかのように、とにかく複雑な構造やツールの要求、というものを排除しました。

.NET 6.0をインストールしている環境なら、

```bash
dotnet tool install -g MarkTheRipper
```

とするだけでインストールできます。または、.NET Framework 4.8に対応した、ポータブルバージョンのバイナリをダウンロードする事もできます。

初めて使う場合は、

```bash
$ mtr new mininum
```

とすると、現在のディレクトリの配下に、以下のようにひな形が生成されます。
（恐れる必要はありません！たった2つ、しかも中身は余計な定義が殆ど存在しない、数行のファイルです！）

* `contents`ディレクトリ: `index.md`ファイルが置かれます。中身は一般的に想定されるマークダウンによる記事テキストです。
* `templates`ディレクトリ: `page-template.html`ファイルが置かれます。サイト生成するときに、マークダウンがHTMLに変換され、このファイルの中に挿入されます。

これだけです！念のために、中身も見せましょう:

### index.md

```markdown
---
title: Hello MarkTheRipper!
author: Kouji Matsui
tags: [foo,bar]
---

# Hello MarkTheRipper!

This is sample post.

## H2

H2 body.

### H3

H3 body.
```

### page-template.html

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="author" content="{author}" />
    <meta name="generator" content="https://github.com/kekyo/MarkTheRipper" />
    <meta name="keywords" content="{tags}" />
    <meta name="referrer" content="same-origin" />
    <meta name="robots" content="index, follow" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{title}</title>
</head>
<body>
    {contentBody}
</body>
</html>
```

中身を見れば、何がどうなるのか想像も付くと思います。MarkTheRipperは、キーワードと本文をHTMLに変換して、テンプレートに挿入しているだけです。
なので、あなたのサイトに向けてカスタマイズする時には、一般的なサイト実装で使われる、様々な技法をそのまま適用でき、制約は殆どありません。

それでは、そのままサイトを生成してみましょう。サイト生成も非常に簡単です:

```bash
$ mtr
```

ディレクトリ構成がサンプルと同じであれば、`mtr`を実行するだけでサイトを生成します。
サイト生成はマルチスレッド・マルチ非同期I/Oで行うので、大量のコンテンツがあっても高速です。
デフォルトでは、`docs`ディレクトリ配下に出力されます。

その後、すぐにデフォルトのブラウザでプレビューが表示されます:

![minimum image](Images/minimum.png)

サイト生成は、毎回`docs`ディレクトリ内のファイルをすべて削除して、生成し直します。
ディレクトリ全体をGitで管理している場合は、`docs`ディレクトを含めてコミットしてOKです。
そうすれば、実際に生成されたファイルの差分を確認することが出来ます。
また、`github.io`にそのままpushして、簡単にあなたのサイトを公開出来ます！

----

## もう少し実用的なサンプル

`mtr new minimum`が生成するサンプルはあまりにシンプルであり（minimumは伊達じゃありません！）、もう少しカスタマイズの例が見たいという場合のために、
標準でサンプルをもう二つ内蔵しています。

```bash
$ mtr new standard
$ mtr new rich
```

`standard`または`rich`というサンプルを指定できます。以下のような機能があります:

* ナビゲーションメニューを追加します（[bootstrap.js 5.0](https://getbootstrap.jp/)）。
* `rich`: 追加の日本語フォント (Noto sans JP) を利用可能にします。
* コードブロックデザイン:
  * `standard`: GitHubのコードブロックのデザインに似た見た目になります。シンタックスハイライトはありません。
  * `rich`: シンタックスハイライトに [prism.js](https://prismjs.com/) を使います。
  
![standard image](Images/standard.png)

![rich image](Images/rich.png)

  ほとんどの場合、これらのサンプルで始めても問題ないと思います。

## Install develop branch package

```
$ dotnet tool install -g MarkTheRipper --nuget-source http://nuget.kekyo.online:59103/repository/nuget/index.json
```

----

## License

Apache-v2.

----

## History

TODO:
