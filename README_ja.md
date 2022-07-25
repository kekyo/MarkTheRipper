# MarkTheRipper

![MarkTheRipper](Images/MarkTheRipper.100.png)

MarkTheRipper - マークダウンで書く事が出来る、静的サイトの高速生成ツール。

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

TODO: 最終的にこのドキュメントは、MarkTheRipper自身で変換する予定です。

MarkTheRipperは、非常にシンプルかつ高速な静的サイトジェネレータで、コンテンツをマークダウンで書くことができます。
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

とすると、現在のディレクトリの配下に、以下のようにサンプルのひな形が生成されます。
（恐れる必要はありません！たった2つ、しかも中身は余計な定義が殆ど存在しない、数行のファイルです！）

* `contents`ディレクトリ: `index.md`ファイルが置かれます。中身は一般的に想定されるマークダウンによる記事テキストです。
* `resources`ディレクトリ: `template-page.html`ファイルが置かれます。サイト生成するときに、マークダウンがHTMLに変換され、このテンプレートファイルの中に挿入されます。

これだけです！念のために、中身も見せましょう:

### index.md

```markdown
---
title: Hello MarkTheRipper!
tags: [foo,bar]
---

# Hello MarkTheRipper!

This is sample post.

## H2

H2 body.

### H3

H3 body.
```

### template-page.html

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="author" content="{author}" />
    <meta name="keywords" content="{tags}" />
    <title>{title}</title>
</head>
<body>
    {contentBody}
    <hr />
    <p>Tags:{foreach:tags} `{tags-item}`{/}</p>
</body>
</html>
```

中身を見れば、何がどうなるのか想像も付くと思います。MarkTheRipperは、キーワードと本文をHTMLに変換して、テンプレートに挿入しているだけです。
なので、テンプレートをカスタマイズする時には、一般的なHTML/JavaScriptの技法をそのまま適用でき、制約は殆どありません。

(MarkTheRipperは.NETで書かれていますが、使用者は.NETの事を知らなくても問題ありません)

それでは、そのままサイトを生成してみましょう。サイト生成も非常に簡単です:

```bash
$ mtr
```

ディレクトリ構成がサンプルと同じであれば、`mtr`を実行するだけでサイトを生成します。
サイト生成はマルチスレッド・マルチ非同期I/Oで行うので、大量のコンテンツがあっても高速です。
デフォルトでは、`docs`ディレクトリ配下に出力されます。
この例では、`contents/index.md`ファイルが、`docs/index.html`ファイルに変換されて配置されます。

その後、すぐにデフォルトのブラウザでプレビューが表示されます:

![minimum image](Images/minimum.png)

サイト生成は、毎回`docs`ディレクトリ内のファイルをすべて削除して、生成し直します。
ディレクトリ全体をGitで管理している場合は、`docs`ディレクトを含めてコミットしてOKです。
そうすれば、実際に生成されたファイルの差分を確認することが出来ます。
また、`github.io`にそのままpushして、簡単にあなたのサイトを公開出来ます！

マークダウンファイルのファイル名や、配置するサブディレクトリにも制約はありません。
`contents`ディレクトリ配下で拡張子が`.md`のファイルがあれば、どのようなサブディレクトリにどのようなファイル名で配置されていても、また、多数存在していても構いません。
サブディレクトリの構造を保ったまま、全ての`.md`ファイルが`.html`ファイルに変換されます。

`.md`ではない拡張子のファイルは、単純に同じ場所にコピーされます。
例えば、写真などの追加ファイルは、あなたが管理したいように配置して、そこを指すように相対パスでマークダウンを書けば良いのです。

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
* コードブロックデザイン:
  * `standard`: GitHubのコードブロックのデザインに似た見た目になります。シンタックスハイライトはありません。
  * `rich`: シンタックスハイライトに [prism.js](https://prismjs.com/) を使います。
  
![standard image](Images/standard.png)

![rich image](Images/rich.png)

心配無用です。これらのサンプルも、最小のテンプレートコードとなるよう、最新の注意を払って実装しました。
理解も容易で、HTMLのビギナーでも、これらのサンプルでカスタマイズを始める事が出来ます。

----

## テンプレートの詳細

MarkTheRipperのテンプレートは、非常にシンプルでありながら、必要十分な柔軟性と応用性を備えています。
テンプレートは、「メタデータ辞書」を参照することで、すべてのキーワード置換を実現します。

テンプレートの置換例を示します。

### キーワードの置き換え

もっとも簡単な例は、単純なキーワードの置き換えです。次のようなテンプレートを定義したとします:

```html
<title>{title}</title>
```

これは、メタデータ辞書の`title`というキーワードに対応する値に置き換えます。
`title`の値はどこにあるかというと、対応するマークダウン文書の先頭で定義します:

```markdown
---
title: Hello MarkTheRipper!
---

(... 本文 ...)
```

他のサイトジェネレーターを試したことがある場合は、マークダウンにこのような特別な「ヘッダ行」を追加して、タイトルや日時などを挿入する方法を知っているかもしれません。
MarkTheRipperも、構文上はこの慣例に従っていますが、もっと柔軟です。例えば、以下の例は全く同じ結果を生成します:

```html
<title>{foobar}</title>
```

```markdown
---
foobar: Hello MarkTheRipper!
---

(... 本文 ...)
```

何となく、メタデータ辞書をどう活用すれば良いか、見えてきましたか？
つまりMarkTheRipperは、マークダウンのヘッダに書いた「キーワードと値」の組をメタデータ辞書として扱うことが出来て、テンプレート上にいくつでも反映することが出来ます。

任意のキーワードを、テンプレート上の任意の箇所で置き換えできるので、例えば以下のような応用が可能です:

```html
<link rel="stylesheet" href="{stylesheet}.css">
```

```markdown
---
title: Hello MarkTheRipper!
stylesheet: darcula
---

(... 本文 ...)
```

恐らく、この機能だけでも、大部分の問題は解決すると思います。

### 特殊なキーワードとフォールバック

このメタデータ辞書には、特殊な、しかし重要と思われるキーワードがいくつか存在します。以下に示します:

|キーワード|内容|
|:----|:----|
|`now`|MarkTheRipperでサイトを生成した日時|
|`template`|適用するテンプレート名|
|`lang`|ロケール(`en-us`や`ja-jp`など)|
|`date`|記事の日時|

これらのキーワードは、マークダウンのヘッダに書いて、上書きする事が出来ます。
`now`を上書きする事に、意味は無いかも知れませんが、
MarkTheRipperがメタデータ辞書の定義を特別扱いしない、と言う事だけ知っておけば問題ありません。

上記のキーワードのうち、`lang`や`template`のデフォルト値は何なのかが気になった人もいると思います。
メタデータ辞書は、サイト生成時のベースとなる定義を、`resources/metadata.json`に配置することが出来ます。
（無くても構いません。実際、minimumサンプルには存在しません）
例えば、以下のような定義です:

```json
{
  "title": "(Draft)",
  "author": "Mark the Ripper",
  "template": "page",
  "lang": "en-us"
}
```

これを見ると面白いことがわかります。`title`キーワードの値が"(Draft)"となっています。例えば、以下のようなテンプレートを考えます:

```html
<meta name="author" content="{author}" />
<title>{title}</title>
```

もし、マークダウンに`title`を指定した場合は、そのタイトルが挿入されますが、指定しなかった場合は"(Draft)"というタイトルが挿入されます。
同様に、`author`を指定した場合は、その投稿者名が挿入されますが、指定しなかった場合は"Mark the Ripper"という投稿者名が挿入されます。

ブログに使うことを考えた場合、殆どの投稿文書はあなた自身が書いたものとなるので、いちいちマークダウンのヘッダに自分の名前を書きたいとは思わないでしょう。
しかし、タイトルはもちろん、その投稿毎に異なるはずです。
そのような場合分けに、この、メタデータ辞書の「フォールバック」機能を使うことが出来ます。

そして、`template`と`lang`のフォールバックですが:

* `template`がフォールバックにも見つからない場合に限り、`page`というテンプレート名が使われます。
* `lang`がフォールバックにも見つからない場合に限り、システムのデフォルト言語が適用されます。

テンプレート名には、少し補足が必要でしょう。テンプレート名は、変換元のテンプレートファイルの特定に使用されます。
例えば、テンプレート名が`page`の場合は、`resources/template-page.html`ファイルが使用されます。もし:

```markdown
---
title: Hello MarkTheRipper!
template: fancy
---
```

のように指定した場合は、`resources/template-fancy.html`が使用されます。

`date`は記事の日時を表していて、普通のキーワードと同様に扱われますが、マークダウンヘッダに定義されていなかった場合は、自動的に生成時の日時が挿入されます。

`lang`は、単に普通のキーワードの一つのようにしか感じられないかも知れません。
これについては、次の節で解説します。

### フォーマットパラメータ

TODO:

### 再帰キーワード検索

TODO:

### イテレーターとネスト

TODO:

----

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
