---
title: ファーストステップ
lang: ja-jp
date: 10/26/2022 22:24:53 +09:00
---

インストール直後に行う最初のステップは、サイトに必要なひな形を準備する事です。
心配には及びません！ MarkTheRipperは、自分自身で簡単にひな形を用意できます。

## サイトのサンプルを生成

初めて使う場合は、

```bash
$ mtr init mininum
```

とすると、現在のディレクトリの配下に、以下のようにサンプルのひな形が生成されます。
（恐れる必要はありません！たった2つ、しかも中身は余計な定義が殆ど存在しない、数行のファイルです！）

* `contents`ディレクトリ: `index.md`ファイルが置かれます。中身は一般的に想定されるマークダウンによる記事テキストです。
* `layouts`ディレクトリ: `page.html`ファイルが置かれます。サイト生成するときに、マークダウンがHTMLに変換され、このレイアウトファイルの中に挿入されます。

これだけです！念のために、中身も見せましょう:

### contents/index.md

```markdown
---
title: Hello MarkTheRipper!
tags: foo,bar
---

This is sample post.

## H2

H2 body.

### H3

H3 body.
```

### layouts/page.html

```html
<!DOCTYPE html>
<html lang="{lang}">
<head>
    <meta charset="utf-8" />
    <meta name="keywords" content="{tags}" />
    <title>{title}</title>
</head>
<body>
    <header>
        <h1>{title}</h1>
        <p>Category:{foreach category.breadcrumbs} {item.name}{end}</p>
        <p>Tags:{foreach tags} {item.name}{end}</p>
        <p>
            {foreach (take (older self) 1)}<a href="{relative item.path}">Older</a>{end}
            {foreach (take (newer self) 1)}<a href="{relative item.path}">Newer</a>{end}
        </p>
    </header>
    <hr />
    <article>
        {contentBody}
    </article>
</body>
</html>
```

中身を見れば、何がどうなるのか想像も付くと思います。MarkTheRipperは、キーワードと本文をHTMLに変換して、レイアウトに挿入しているだけです。
なので、レイアウトをカスタマイズする時には、一般的なHTML/CSS/JavaScriptの技法をそのまま適用でき、制約は殆どありません。

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

## 普段の操作

MarkTheRipperは、`contents`ディレクトリ以下に配置された、全てのマークダウンファイルを自動的に認識するため、
何か記事を書く場合は、単純に好きなディレクトリに好きなようにマークダウンファイルを作って記述すればOKです。

この操作すら面倒ですか？ ええ、もちろん知っています。なので、もっと簡単にマークダウンファイルを作って配置して、記事を書くことに集中できる方法があります。

```bash
$ mtr new
```

これで、`contents`ディレクトリの直下に、現在の日付を元にした、ひな形のマークダウンファイルが生成され、かつデフォルトのマークダウンエディタが自動的に開きます。
カテゴリについては後で詳しく解説しますが、記事をカテゴリに配置したい場合は:

```bash
$ mtr new foo/bar/baz
```

とすれば、カテゴリ「foo」の「bar」の更に「baz」に、記事が配置されます。

## もう少し実用的なサンプル

`mtr init minimum`が生成するサンプルはあまりにシンプルであり（minimumは伊達じゃありません！）、もう少しカスタマイズの例が見たいという場合のために、
標準でサンプルをいくつか内蔵しています。

```bash
$ mtr init sidebar
$ mtr init standard
$ mtr init rich
```

`sidebar`、`standard`または`rich`というサンプルを指定できます。以下のような機能があります:

|名称|内容|
|:----|:----|
|`sidebar`|`minimum`サンプルに、CSS flexによるサイドバーナビゲーションを追加したものです。全体的に一からデザインしたい場合は、余計な定義が一切含まれていないので、都合がよいでしょう。|
|`standard`|GitHubのコードブロックのデザインに似た見た目になります。シンタックスハイライトはありません。[bootstrap.js 5.0](https://getbootstrap.jp/)を使用しています。|
|`rich`|シンタックスハイライトに [prism.js](https://prismjs.com/) を使います。これも、bootstrap.js 5.0 を使っています。|

![standard image](Images/standard.png)

![rich image](Images/rich.png)

心配無用です。これらのサンプルも、最小のレイアウトコードとなるよう、最新の注意を払って実装しました。
理解も容易で、HTMLのビギナーでも、これらのサンプルでカスタマイズを始める事が出来ます。
