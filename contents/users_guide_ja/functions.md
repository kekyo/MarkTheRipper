---
title: 組み込みの関数群
lang: ja-jp
date: 10/26/2022 22:24:56 +09:00
---

MarkTheRipperに組み込まれた関数の一覧を示します:

|関数|内容|
|:----|:----|
|`format`|引数を文字列に整形します。|
|`relative`|引数のパスを、相対パスに変換します。|
|`lookup`|引数が示す結果を元に、メタデータ辞書を引きます。|
|`older`|引数が示す記事よりも古い記事を列挙します。|
|`newer`|引数が示す記事よりも新しい記事を列挙します。|
|`take`|列挙可能数を制限します。|
|`add`|引数を加算します。|
|`sub`|引数を減算します。|
|`mul`|引数を乗算します。|
|`div`|引数を除算します。|
|`mod`|引数の剰余を取得します。|
|`embed`|[oEmbedプロトコル](https://oembed.com/)などを使用して、埋め込みコンテンツを生成します。|
|`card`|[OGPメタデータ](https://ogp.me/)などを使用して、カード形式のコンテンツを生成します。|

## format

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

## relative

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

## lookup

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
  "diary": "今日起きたこと"
}
```

のようなキーワードを登録しておけば、実際に出力される文字列を入れ替えることが出来ます。
これまで見てきたように、関数の引数に直接値を指定することも出来るため、固定の文字列を出力したい場合でも:

```html
<p>Tag: {lookup 'blog'}</p>
```

のようにすれば、置き換えが可能です。

## older, newer (記事のナビゲーション)

`older`や`newer`を使用すると、指定された記事よりも古い記事や、新しい記事を列挙することが出来ます。
例えば、現在の記事よりも新しい記事を列挙するには、以下の式を使います:

```html
{foreach (newer self)}<p>{item.title}</p>{end}
```

ここで、`self`は現在の記事（このレイアウトを使用中のマークダウン記事）を示し、`foreach`が列挙する個々の要素が、新しい記事を示します。
従って、以下のようにプロパティを参照して、`relative`関数を応用して、リンクを作ることが出来ます:

```html
{foreach (newer self)}<p><a href="{relative item.path}">{item.title}</a></p>{end}
```

括弧の位置に注意しましょう。上記では、`newer`関数に`self`を引数として指定するために、括弧を使用しています。
括弧を指定しないと、`foreach`に`newer`と`self`を引数として指定したことになり、正しく動作しません。

列挙される記事は、その記事が属するカテゴリで、日付順に得られます。
そのため、MarkTheRipperでは、日付順を意識する「ブログ」のような記事を、
`blog`のようなカテゴリに分類して、そのカテゴリ内でこの関数を使用してナビゲーションする事をお勧めします。

`older`や`newer`の引数には、`self`以外にも、記事を示す式を指定できます。
`entries`プロパティを列挙した値が相当します。

## take (列挙操作)

`take`は、列挙可能な値の列挙数を制限します。
例えば、`tag.entries`は、そのタグを持つ全ての記事を列挙します:

```html
{foreach tags tag}
<h2>{tag}</h2>
{foreach tag.entries}<p>{item.title}</p>{end}
{end}
```

しかし、これを数個に制限したい事もあるでしょう:

```html
{foreach tags tag}
<h2>{tag}</h2>
{foreach (take tag.entries 3)}<p>{item.title}</p>{end}
{end}
```

`take`関数は2つの引数を指定します。1個目は列挙対象、2個目は制限する列挙数です。上記の式では、最大3個に制限しています。

この`take`関数と、`foreach`を使用した、ちょっとしたテクニックがあります。前節の`newer`関数と組み合わせて応用します:

```html
{foreach (take (newer self) 1)}<a href="{relative item.path}">Newer: {item.title}</a>{end}
```

列挙する個数を1個に制限するという事は、もし0個の場合は全く列挙されないと言う事です。
つまり、列挙する記事が無い場合は、`foreach`の中が出力されないため、表示させない事が出来ます。
それにより、現在の記事よりも新しい記事が存在しない場合は、リンクが表示されなくなります。

この式は、ほぼ同じものがサンプルのレイアウトにあります。

## add, sub, mul, div, mod (計算全般)

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
<p>1 + 3 = {format (add 1 3) 'D3'}</p>
```

結果:

```html
<p>1 + 3 = 004</p>
```

引数が数値ではなくても、文字列が数値として見なすことが出来ればOKです:

```html
<p>1 + 2 + 3 = {add 1 '2' 3}</p>
```

引数の中に、小数を含む数値があっても構いません。その場合は小数を含む計算として処理されます(浮動小数点演算と呼びます):

```html
<p>1 + 2.1 + 3 = {add 1 2.1 3}</p>
```

小数を含む結果は、意図したとおりの表示にならない事があります。
そのような可能性を考えて、常に`format`関数を使った方が良いかもしれません。
小数を含む書式の指定方法は、[ここを参照して下さい](https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/standard-numeric-format-strings#fixed-point-format-specifier-f)。

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

結果:

```html
<p>index/count = 1/3</p>
<p>index/count = 2/3</p>
<p>index/count = 3/3</p>
```

## embed (埋め込みコンテンツを生成)

`embed` (と後述の`card`) 関数は特殊で、単純な計算を行うのではなく、
外部データを参照してコンテンツを生成する、強力な関数です。

あなたは、自分のブログにYouTubeの動画を埋め込みたいと考えたことはありませんか？
あるいは、単なるリンクではなく、外部コンテンツへのカード形式のコンテンツを表示したいと考えたこともあるかも知れません。

`embed`と`card`関数は、このような複雑なコンテンツ埋め込みを容易に実現します。
例えば、文書中に以下のように書きます:

```markdown
## すばらしい動画を発見

いつか、自由に旅行できるようになったら、訪れてみたい...

{embed https://youtu.be/1La4QzGeaaQ}
```

すると、以下のように表示されます:

![embed-sample1-ja](Images/embed-sample1-ja.png)

この画像は、単なるサムネイルではありません。実際にページ内で動画を再生することもできます。
マジック！ですか？ これは、業界標準の[oEmbedプロトコル](https://oembed.com/)を使って、
HTMLに埋め込むべきコンテンツを自動的に収集して実現しています。

`embed`関数の引数には、そのコンテンツの「パーマリンク」、つまり共有すべきURLを指定するだけです。
YouTubeの他にも、数多くの有名コンテンツサイトが対応しているため:

```markdown
## Today's DIVISION

{embed https://twitter.com/kekyo2/status/1467073038667882497}
```

![embed-sample2-ja](Images/embed-sample2-ja.png)

このように、Twitterなど、他のコンテンツも簡単に埋め込めます。
どのコンテンツサイトが対応しているのかは、[直接oEmbedのjsonメタデータを参照](https://oembed.com/providers.json)して確かめてみてください。
かなり多くのサイトが対応済みであることが分かります。

しかし、私たちが知りうる有用なサイトの一つであるAmazonは、何とoEmbedに対応していません！
そのため、MarkTheRipperでは、Amazonの商品リンクを特別に認識して、同様に埋め込めるようにしています:

```markdown
## USBホストを行う実験

{embed https://amzn.to/3V6lYlQ}
```

![embed-sample3-ja](Images/embed-sample3-ja.png)

* このリンクは、Amazon associatesを有効化する事で、取得出来るようになります。
  Amazon associatesは、Amazonのアカウントがあれば誰でも有効化出来ますが、ここでは詳細を省きます。

さて、この便利な関数を使用するには、少しだけ準備が必要です。
この埋め込みコンテンツを表示させるための、専用のレイアウトファイル`layouts/embed.html`を用意します。
内容は以下の通りです:

```html
<div style="max-width:800px;margin:10px;">
    {contentBody}
</div>
```

これまでのレイアウト解説と同様に、`contentBody`には、実際に埋め込むべきoEmbedのコンテンツが埋め込まれます。
外側の`div`タグは、この埋め込みコンテンツの領域を決めるものです。
上記では外周に若干空白を持たせて、本体は横が800pxとなるようにしています。
あなたのサイトのデザインに合わせて、調整すると良いでしょう。

もし、コンテンツサイトごとに異なるレイアウトが必要な場合は、`layouts/embed-YouTube.html`のように、
oEmbedプロバイダ名を指定したファイル名とする事で、サイト固有のカスタマイズが出来ます。

ところで、oEmbedプロトコルで得られる情報には、埋め込みコンテンツが含まれていない場合があります。
そのような場合は、一緒に取得出来たoEmbedメタデータを使用して、次に紹介する`card`関数と同様のコンテンツを生成します。

## card (カードコンテンツを生成)

`embed`関数は、コンテンツプロバイダーが用意した埋め込みコンテンツを直接表示させるものでした。
この`card`関数は、コンテンツのメタデータを収集して、MarkTheRipper側で用意したビューで表示させます。

メタデータは、以下の方法で収集します:

* oEmbed: 付随するメタデータを使用（`embed`関数で埋め込みコンテンツが提供されなかった場合を含む）
* OGP (Open Graph protocol): 対象のページをスクレイピングし、ページに含まれるOGPメタデータを収集。
* Amazon: Amazon associatesページから収集。

使い方は`embed`関数と全く同じです:

```markdown
## すばらしい動画を発見

いつか、自由に旅行できるようになったら、訪れてみたい...

{card https://youtu.be/1La4QzGeaaQ}
```

すると、以下のように表示されます:

![card-sample1-ja](Images/card-sample1-ja.png)

`embed`関数と異なり、付加情報をカード状にまとめた、コンテンツとリンクとして表示します。
同様に:

```markdown
## USBホストを行う実験

{card https://amzn.to/3V6lYlQ}
```

![card-sample3-ja](Images/card-sample3-ja.png)

様々なコンテンツを、同じカード形式で表示できます。
埋め込み形式とカード形式のどちらを使うかは、好みで使い分ければ良いでしょう。

`card`関数も、`embed`関数と同様に、専用のレイアウトファイルを用意する必要があります。
レイアウトファイル`layouts/card.html`も、以下のひな形を元に、自分のサイトに合わせていけば良いでしょう:

```html
<div style="max-width:640px;margin:10px;">
    <ul style="display:flex;padding:0;border:1px solid #e0e0e0;border-radius:5px;">
        <li style="min-width:180px;max-width:180px;padding:0;list-style:none;">
            <a href="{permaLink}" target="_blank" style="display:block;width:100%;height:auto;color:inherit;text-decoration:inherit;">
                <img style="margin:10px;width:100%;height:auto;" src="{imageUrl}" alt="{title}">
            </a>
        </li>
        <li style="flex-grow:1;margin:10px;list-style:none; ">
            <a href="{permaLink}" target="_blank" style="display:block;width:100%;height:auto;color:inherit;text-decoration:inherit;">
                <h5 style="font-weight:bold;">{title}</h5>
                <p>{author}</p>
                <p>{description}</p>
                <p><small class="text-muted">{siteName}</small></p>
            </a>
        </li>
    </ul>
</div>
```

このひな形は、完全に独立したHTMLになっています。もしBootstrapと併用したいのであれば、
`mtr init standard`や`mtr init rich`で生成されるサンプルレイアウトに含まれるファイルを参照して下さい。

`card`関数でも、`layouts/card-YouTube.html`のようなファイル名にする事で、サイト固有のカスタマイズが出来ます。
