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
$ mtr init mininum
```

とすると、現在のディレクトリの配下に、以下のようにサンプルのひな形が生成されます。
（恐れる必要はありません！たった2つ、しかも中身は余計な定義が殆ど存在しない、数行のファイルです！）

* `contents`ディレクトリ: `index.md`ファイルが置かれます。中身は一般的に想定されるマークダウンによる記事テキストです。
* `resources`ディレクトリ: `layout-page.html`ファイルが置かれます。サイト生成するときに、マークダウンがHTMLに変換され、このレイアウトファイルの中に挿入されます。

これだけです！念のために、中身も見せましょう:

### index.md

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

### layout-page.html

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

### 普段の操作

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

----

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

----

## レイアウトの詳細

MarkTheRipperのレイアウトは、非常にシンプルでありながら、必要十分な柔軟性と応用性を備えています。
レイアウトは、「メタデータ辞書」を参照することで、すべてのキーワード置換を実現します。

レイアウトの置換例を示します。

----

### キーワードの置き換え

もっとも簡単な例は、単純なキーワードの置き換えです。次のようなレイアウトを定義したとします:

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

* このヘッダの事を、他のサイトジェネレーターでは「FrontMatter」と呼び、YAML構文で記述することになっていますが、
  MarkTheRipperは厳密にはYAMLではなく、記述の柔軟性を高めるための構文を使用しています。
  例えば、`title`は、ダブルクオートで括る事は必須ではありません。
  念のため、MarkTheRipperでは、FrontMatterという用語を使っていません。

何となく、メタデータ辞書をどう活用すれば良いか、見えてきましたか？
つまりMarkTheRipperは、マークダウンのヘッダに書いた「キーワードと値」の組をメタデータ辞書として扱うことが出来て、レイアウト上にいくつでも反映することが出来ます。

任意のキーワードを、レイアウト上の任意の箇所で置き換えできるので、例えば以下のような応用が可能です:

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

----

### 特殊なキーワードとフォールバック

このメタデータ辞書には、特殊な、しかし重要と思われるキーワードがいくつか存在します。以下に示します:

|キーワード|内容|
|:----|:----|
|`generated`|MarkTheRipperでサイトを生成した日時|
|`layout`|適用するレイアウト名|
|`lang`|ロケール(`en-us`や`ja-jp`など)|
|`date`|記事の日時|
|`timezone`|MarkTheRipperでサイトを生成した環境のタイムゾーン。IANA表記、または時間|
|`published`|明示的に`false`と指定する事で、このマークダウンを無視する|

* この他にもいくつか特殊なキーワードがありますが、後で解説します。

これらのキーワードは、マークダウンのヘッダに書いて、上書きする事が出来ます。
`generated`を上書きする事に意味は無いかも知れませんが、
MarkTheRipperがメタデータ辞書の定義を特別扱いしない、と言う事だけ知っておけば問題ありません。

上記のキーワードのうち、`lang`や`layout`や`timezone`のデフォルト値は何なのかが気になった人もいると思います。
メタデータ辞書は、サイト生成時のベースとなる定義を、`resources/metadata.json`に配置することが出来ます。
（無くても構いません。実際、minimum/sidebarサンプルには存在しません）
例えば、以下のような定義です:

```json
{
  "title": "(Draft)",
  "author": "Mark the Ripper",
  "layout": "page",
  "lang": "ja-jp",
  "timezone": "Asia/Tokyo",
}
```

これを見ると面白いことがわかります。`title`キーワードの値が"(Draft)"となっています。例えば、以下のようなレイアウトを考えます:

```html
<meta name="author" content="{author}" />
<title>{title}</title>
```

もし、マークダウンに`title`を指定した場合は、そのタイトルが挿入されますが、指定しなかった場合は"(Draft)"というタイトルが挿入されます。
同様に、`author`を指定した場合は、その投稿者名が挿入されますが、指定しなかった場合は"Mark the Ripper"という投稿者名が挿入されます。

ブログに使うことを考えた場合、殆どの投稿文書はあなた自身が書いたものとなるので、いちいちマークダウンのヘッダに自分の名前を書きたいとは思わないでしょう。
しかし、タイトルはもちろん、その投稿毎に異なるはずです。
そのような場合分けに、この、メタデータ辞書の「フォールバック」機能を使うことが出来ます。

そして、`layout`と`lang`のフォールバックですが:

* `layout`がフォールバックにも見つからない場合に限り、`page`というレイアウト名が使われます。
* `lang`がフォールバックにも見つからない場合に限り、システムのデフォルト言語が適用されます。
* `timezone`がフォールバックにも見つからない場合に限り、システムのタイムゾーン設定が適用されます。

レイアウト名には、少し補足が必要でしょう。レイアウト名は、変換元のレイアウトファイルの特定に使用されます。
例えば、レイアウト名が`page`の場合は、`resources/layout-page.html`ファイルが使用されます。もし:

```markdown
---
title: Hello MarkTheRipper!
layout: fancy
---

(... 本文 ...)
```

のように指定した場合は、`resources/layout-fancy.html`が使用されます。

`date`は記事の日時を表していて、普通のキーワードと同様に扱われますが、
マークダウンヘッダに定義されていなかった場合は、自動的に生成時の日時が挿入されます。

`timezone`は、`date`のような日時を扱う場合に参照され、タイムゾーン補正を行う基準となります。
MarkTheRipperを動作させる環境の、システムのタイムゾーン設定が、日常的に記事に埋め込む日時と異なる場合は、
`metadata.json`ファイルに含めておくと良いでしょう
（このような代表的な環境として、GitHub Actionsなどのクラウドサービス上で動作させる場合が考えられます）。

`lang`は、単に普通のキーワードの一つのようにしか感じられないかも知れません。
これについては、後の節で解説します。

----

### 再帰キーワード検索

メタデータ辞書で引いた結果をキーワードとして、再度メタデータ辞書から引きたいと思うことがあります。例えば:

```markdown
---
title: Hello MarkTheRipper!
category: blog
---

(... 本文 ...)
```

`category`とは、記事のカテゴリです。ここでは、`blog`と名付けていますが、以下のようにキーワード参照した場合:

```html
<p>Category: {category}</p>
```

HTMLには `Category: blog` のように表示されます。これで問題ない場合もありますが、もっと丁寧な文に置き換えたいかもしれません。
そこで、`blog`をキーワードとして、再度メタデータ辞書から値を検索させることが出来ます。
MarkTheRipperに組み込まれている、`lookup` 関数キーワードを使用します:

```html
<p>Category: {lookup category}</p>
```

* 関数キーワードの詳細については、後の章で説明します。

こうしておいて、メタデータ辞書に、`blog`と`私的な日記` を対にして登録しておけば、HTMLには `Category: 私的な日記` と表示されます。

このようなキーワードと値のペアは、前節で示した`resources/metadata.json`に書いておけば参照出来るようになります。
加えて、実はメタデータ辞書のファイルは、`resources/metadata*.json`でマッチするすべてのJSONファイルが対象です。
ファイルが分かれていても、MarkTheRipperが起動する時に、すべて読み込まれて内容がマージされます。

例えば、記事カテゴリだけを管理するファイルとして、`resource/metadata-category.json`のように別のファイルにしておけば、管理が容易になるでしょう。

----

### 列挙とネスト

タグやカテゴリのような分類は、メニューから選択させてそのページに遷移させたいと思うでしょう。
例えば、サイト全体でタグが5個あったとします。これをページのメニューに自動的に加えて、
メニューからタグに分類されたページに遷移できるようにするには、「列挙機能」を使います。

例によって、小さい例から始めましょう。これはminimumに含まれているレイアウトです:

```html
<p>Tags:{foreach tags} '{item}'{end}</p>
```

* `tags`キーワードは、タグのリストを示します（後述）

これは、`{foreach tags}`と`{end}`の間にある文書が、`tags`の個数だけ繰り返し出力されるというものです。
「間にある文書」とは、ここでは ` '{item}'` の事です。スペースが含まれてることに注意してください。
同様に、改行やHTMLのタグなど、この間には何を含んでいても構いません。

では、以下のようなマークダウンを変換したとします:

```markdown
---
title: Hello MarkTheRipper
tags: foo,bar,baz
---

(... 本文 ...)
```

すると、`<p>Tags: 'foo' 'bar' 'baz'</p>` と出力されます。
`tags`の`foo,bar,baz`が、スペースで区切られて展開されて、クオートされて出力されました。

`{foreach tags}`と`{end}`の間にある文書が繰り返し出力されるので、以下のように使う事も出来ます:

```html
<ul>
  {foreach tags}
  <li>{item.index}/{item.count} {item}</li>
  {end}
</ul>
```

結果:

```html
<ul>
  <li>0/3 foo</li>
  <li>1/3 bar</li>
  <li>2/3 baz</li>
</ul>
```

間に挿入されている`{item}`は、繰り返される一つ一つの値を参照できるキーワードです。
また、`{item.index}` と指定すると、0から始まって1,2,3... とカウントする数値が得られます。
`{item.count}` は、繰り返しの個数です。上記ではタグが3個あるため、この値は常に3となります。

さらに、複数のキーワードをネストさせる事も出来ます。以下の例は、タグを2重に繰り返します:

```html
<ul>
  {foreach tags}
  {foreach tags}
  <li>{item.index} {item}</li>
  {end}
  {end}
</ul>
```

結果:

```html
<ul>
  <li>0 foo</li>
  <li>1 bar</li>
  <li>0 foo</li>
  <li>1 bar</li>
</ul>
```

ところで、この場合の`item`は、2重にネストした内側の`tags`の繰り返しを指している事に注意して下さい。
場合によっては、外側の繰り返しの値を使いたいと思うかもしれません。
その場合は、`foreach`に「束縛名」を指定します:

```html
<ul>
  {foreach tags tag1}
  {foreach tags tag2}
  <li>{tag1.index}-{tag2.index} {tag1}/{tag2}</li>
  {end}
  {end}
</ul>
```

結果:

```html
<ul>
  <li>0-0 foo/foo</li>
  <li>0-1 foo/bar</li>
  <li>1-0 bar/foo</li>
  <li>1-1 bar/bar</li>
</ul>
```

束縛名を省略した場合は、`item`が使用されます。
これで、`foreach`による繰り返しの使い方が把握できたと思います。

----

### タグの集約

繰り返しの使い方さえ理解できれば、タグやカテゴリの操作は出来たも同然です。
MarkTheRipperは、コンテンツのすべてのタグとカテゴリの分類を、自動的に集約します。
タグについては、以下の特殊なキーワードで参照できます:

|キーワード|内容|
|:----|:----|
|`tags`|タグのリスト。各マークダウンのヘッダ部分に記述する|
|`tagList`|すべてのタグを集約したリスト|

まずはタグの一覧を作ってみましょう:

```html
<ul>
  {foreach tagList tag}
  <li>{tag}</li>
  {end}
</ul>
```

結果:

```html
<ul>
  <li>foo</li>
  <li>bar</li>
  <li>baz</li>
       :
</ul>
```

前節では、個々のマークダウンに定義された`tags`を使って繰り返しましたが、ここでは`tagList`を使っています。
この違いは、1つのマークダウンファイルではなく、MarkTheRipperが処理する全てのマークダウンのタグを集計した結果を扱っている事です。

つまり、`tagList`を使えば、タグによるメニュー項目やリンクリストを追加出来るようになります。
各タグの項目に、リンクを加えるにはどうすれば良いでしょうか？
タグだけでは、タグに紐づいたコンテンツ群は分かりませんが、実はタグは`foreach`で列挙することが出来ます:

```html
{foreach tagList tag}
<h1>{tag}</h1>
{foreach tag.entries entry}
<h2><a href="{entry.path}">{entry.title}</a></h2>
{end}
{end}
```

束縛名を指定して、何を列挙しようとしているのか分かりやすくしていることに注意してください。

`entries`プロパティを列挙すると、対応するマークダウン群の情報にアクセスできます。
この例のように`path`というプロパティを使用すると、コンテンツに対応するファイルへのパスが得られ、
`title`を使用すれば、そのタイトル（マークダウンのヘッダに記述された`title`）が得られます。

* `path`は、マークダウンへのパスではなく、変換されたHTMLファイルへのパスが得られます。

ところで、このパスは、出力ディレクトリを基準とした相対パスです。
HTMLにパスを埋め込む場合、HTMLファイルが存在するディレクトリからの相対パスである必要があります。
この計算を行うには、MarkTheRipper内蔵の関数キーワード `relative` を使います:

```html
<h2><a href="{relative entry.path}">{entry.title}</a></h2>
```

`relative`を使ってパスを計算すると、MarkTheRipperが出力したHTMLがどのサーバーにどのようにデプロイされたとしても、正しくリンクが機能するようになります。
ハードコーディングされたサイトの基準パスを使用するよりも、安全となるでしょう。
  
----

### カテゴリの集約

カテゴリについては、以下の特殊なキーワードで参照できます:

|キーワード|内容|
|:----|:----|
|`category`|カテゴリの階層リスト。各マークダウンのヘッダ部分に記述する|
|`rootCategory`|ルート(分類なし)となるカテゴリ|

タグとカテゴリが決定的に異なるのは、タグは並行して定義されるものであり、カテゴリは階層化を伴って定義されるものである事です。例えば:

```
(root) --+-- foo --+-- bar --+-- baz --+-- foobarbaz1.md
         |         |         |         +-- foobarbaz2.md
         |         |         |
         |         |         +-- foobar1.md
         |         |
         |         +--- foo1.md
         |         +--- foo2.md
         |         +--- foo3.md
         |
         +--- blog1.md
         +--- blog2.md
```

上の例では、`foobarbaz1.md`は、カテゴリ`foo/bar/baz`に属しています。
また、`blog1.md`は、どのカテゴリにも属していません。
MarkTheRipper内部では、無名の`(root)`カテゴリに属することになっています。
これが、`rootCategory`キーワードです。

タグの場合は`tags`キーワードを使用して定義しましたが、カテゴリの場合は`category`というキーワードを使用します。
上記の`foobarbaz1.md`に相当する定義は:

```markdown
---
title: Hello MarkTheRipper
category: foo,bar,baz
---

(... 本文 ...)
```

のように、階層をリストで指定します。タグと異なり、このリストは階層を表していることに注意してください。
CMSやサイトジェネレーターではこのような階層構造を、しばしば「パンくずリスト(breadcrumb)」と呼ぶことがあります。

ところで、MarkTheRipperは、このようにいちいち`category`キーワードでカテゴリを明示しなくても、コンテンツをカテゴリ分けされたサブディレクトリに配置するだけで、ディレクトリ名からカテゴリを決定出来ます。
従って、カテゴリによるコンテンツの分類は、サブディレクトリで区分けするだけで良いのです。

カテゴリの基本的な構造を把握出来たと思うので、実際にレイアウトを書いてみましょう。まずはルートカテゴリを列挙します:

```html
<h1>{rootCategory.name}</h1>
<ul>
  {foreach rootCategory.entries entry}
  <li>{entry.path}</li>
  {end}
</ul>
```

結果:

```html
<h1>(root)</h1>
<ul>
  <li>blog1.html</li>
  <li>blog2.html</li>
</ul>
```

`rootCategory`はルートカテゴリを表しているため、そのプロパティ`name`は`(root)`になります。
この名称が表示にふさわしくない場合は、キーワードの再帰検索を使って置き換えるか、またはこの例ではルートなので、直接書いてしまっても良いと思います。

そして、タグの時と同様に`entries`で列挙したそれぞれの要素から、各マークダウンのヘッダ情報を引き出すことが出来ます。
ここでは`path`を指定して、コンテンツのパスを出力していますが、`title`とすればタイトルが出力出来て、`item.path.relative`とすれば、現在のコンテンツからの相対パスが得られるので、これをそのままリンクのURLにすることで、リンクを実現できます。

カテゴリを列挙するには、`children`プロパティを使います:

```html
<h1>{rootCategory.name}</h1>
{foreach rootCategory.children child1}
<h2>{child1.name}</h2>
{foreach child1.children child2}
<h3>{child2.name}</h3>
{end}
{end}
```

列挙のネストを増やしていけば、深いカテゴリ構造を全て列挙することが出来ます。
残念ながら、カテゴリ構造を動的に列挙する、つまり存在する子孫カテゴリまでを自動的に再帰的に列挙させる事は出来ません。
これは設計上の制約で、MarkTheRipperには、関数と再帰関数を定義する能力が無いためです。
（そのような要求は、恐らくサイト全体の構造リストを出力する場合だけであり、必要性を感じなかったためです）

カテゴリ操作の最後に、パンくずリストを出力する例を示します。これは非常に簡単です:

```html
<ul>
  {foreach category.breadcrumbs}
  <li>{item.name}</li>
  {end}
</ul>
```

`breadcrumbs`プロパティは、対象のカテゴリに至るカテゴリを、ルートから列挙出来る値を返します。
（但し、対象のカテゴリがルートの場合は、ルートカテゴリを含み、それ以外の場合は含みません）

列挙した個々の要素は、今まで説明してきたカテゴリと同様です。上記例では`name`プロパティでカテゴリ名を出力しています。

----

### 関数キーワード

これまでに出てきた関数を含めた、組み込み関数の一覧を示します:

|関数|内容|
|:----|:----|
|`format`|引数を文字列に整形します。|
|`relative`|引数のパスを、相対パスに変換します。|
|`lookup`|引数が示す結果を元に、メタデータ辞書を引きます。|
|`add`|引数を加算します。|
|`sub`|引数を減算します。|
|`mul`|引数を乗算します。|
|`div`|引数を除算します。|
|`mod`|引数の剰余を取得します。|

#### format

引数を文字列に整形と言っても、`format`を使わずとも、これまでも特に問題なく整形出来ていました。
この関数が特に有用な場合は、日時を扱う場合です。

まず、書式がどのように決まるのかを示します。このようなHTMLで、`date`キーワードや`generated`キーワードを使う場合:

```html
<p>Date: {date}</p>
```

以下の書式で日時が出力されます:

```markdown
---
date: 2022/1/23 12:34:56
lang: ja-jp
---

(... 本文 ...)
```

```html
<p>Date: 2022/01/02 12:34:56 +09:00</p>
```

この書式は、`lang`が示す言語によって変化します。`ja-jp`ではなく`en-us`の場合は:

```html
<p>Date: 1/2/2022 12:34:56 PM +09:00</p>
```

のように、その言語の標準的な書式に従います。
また、文字列に整形する際に `timezone` キーワードの値が参照され、タイムゾーン補正された日時が出力されます。

書式を固定したい場合は、以下のように`format`関数を使用します:

```html
<p>Date: {format date 'yyyy-MM-dd HH.mm.ss'}</p>
```

```html
<p>Date: 2022-01-02 12.34.56</p>
```

`format`関数の第一引数は、整形したい値を示す式、第二引数は書式指定文字列です。
書式指定文字列は、シングルクオートかダブルクオートで括ります。

以前に、「MarkTheRipperは.NETで書かれていますが、使用者は.NETの事を知らなくても問題ありません」と書きましたが、
この書式指定文字列だけは、.NETの慣例に従っています。
日時の正確な書式指定文字列の形式は、[このドキュメントを参照してください](https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/standard-date-and-time-format-strings)。

実際には、日時に限らず、あらゆる値を書式に従って整形出来ます。
例えば、列挙中の`index`は数値ですが、以下のようにすれば:

```html
<p>{format item.index 'D3'}</p>
```

数値を3桁にすることが出来ます。

```html
<p>007</p>
```

様々な書式指定の形式も、上に挙げた.NETドキュメントの周辺に詳しく掲載されています。

#### relative

既に解説した通り、`relative`関数は現在の記事のパスからの相対パスを計算します。

```html
<p>{relative item.path}</p>
```

関数の引数には、文字列を渡すこともできます。例えば:

```html
<link rel="stylesheet" href="{relative 'github.css'}">
```

このようにすれば、スタイルシートが存在する正しい相対パスを指定出来ます。
引数に指定するパスは、サイトのルートディレクトリ、`docs`からの相対位置です。
上記例では、`docs/github.css`への相対パスとなります。

通常このような場合は、絶対パスを指定します。
しかし絶対パスを使った場合、サイトのデプロイ環境によっては、無効なパスとなってしまいます。
`relative`関数を使用すれば、生成されたコンテンツのポータビリティが向上します。

#### lookup

`lookup`関数は、引数に指定された値と同じ名称のキーワードを、再度メタデータ辞書から検索します。
典型的な使用例は、タグやカテゴリの名称を、メタデータ辞書から引き直すというものです:

```html
<p>Tag: {lookup tag}</p>
```

`{tag}`の場合は、タグの真の名称が出力されますが、`lookup`関数を使った場合は、
メタデータ辞書からタグ名と同じキーワードの値を取得して出力します。
従って、`metadata.json`に:

```json
{
     :
  "diary": "今日起きたこと",
     :
}
```

のようなキーワードを登録しておけば、実際に出力される文字列を入れ替えることが出来ます。
これまで見てきたように、関数の引数に直接値を指定することも出来るため、固定の文字列を出力したい場合でも:

```html
<p>Tag: {lookup 'blog'}</p>
```

のようにすれば、置き換えが可能です。

#### add, sub, mul, div, mod (計算全般)

これらは計算を行う関数です。引数は1個以上必要で、3個以上の場合は、連続して計算を行います。例えば:

```html
<p>1 + 2 + 3 = {add 1 2 3}</p>
```

のように、引数の数値を全て加算します。
複雑な計算を行う場合は、括弧を使います:

```html
<p>(1 + 2) * 4 = {mul (add 1 2) 4}</p>
```

括弧はいくつでもネスト出来ます。括弧を応用して、`format`関数を使って望ましい形に整形出来ます:

```html
<p>1 / 3 = {format (div 1 3) 'F3'}</p>
```

結果:

```html
<p>1 / 3 = 0.333</p>
```

小数を含む結果は、意図したとおりの表示にならない事があります。
そのような可能性を考えて、常に`format`関数を使った方が良いかもしれません。
小数を含む書式の指定方法は、[ここを参照して下さい](https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/standard-numeric-format-strings#fixed-point-format-specifier-f)。

引数が数値ではなくても、文字列が数値として見なすことが出来ればOKです:

```html
<p>1 + 2 + 3 = {add 1 '2' 3}</p>
```

引数の中に、小数を含む数値があっても構いません。その場合は小数を含む計算として処理されます(浮動小数点演算と呼びます):

```html
<p>1 + 2.1 + 3 = {add 1 2.1 3}</p>
```

計算を使用する簡単な例を示します。列挙中の`item.index`は、0から開始される数値です。
一方、`item.count`は列挙できる数ですが、これを並べると一般的な表記となりません:

```html
<p>index/count = {item.index}/{item.count}</p>
```

結果:

```html
<p>index/count = 0/3</p>
<p>index/count = 1/3</p>
<p>index/count = 2/3</p>
```

このような場合に、`add`を使って:

```html
<p>index/count = {add item.index 1}/{item.count}</p>
```

とすれば、1から`count`までの数値となり、自然な表現に近づける事が出来ます。

----

## マークダウン中のキーワードの置き換え

これまでに説明してきたキーワードの置き換えは、レイアウトファイルに対して行うというものでした。
このキーワード置き換え機能は、マークダウンファイルにも同様に適用されます。例えば:

```markdown
---
title: hoehoe
tags: foo,bar,baz
---

Title: {title}
```

このようなマークダウンを記述すると、`{title}`が同じようにキーワード置換されます。
もちろんこれまでに説明してきた、関数キーワードによる計算も可能です。

マークダウン上のキーワード置き換えは、コードブロックに対しては機能しません:

````markdown
---
title: hoehoe
tags: foo,bar,baz
---

Title: `{title}`

```
{title}
```
````

上記のように、コードブロック内に配置された`{...}`は、MarkTheRipperで解釈されずに、そのまま出力されます。

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
