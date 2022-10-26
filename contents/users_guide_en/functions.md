---
title: Built-in functions
lang: en-us
date: 10/26/2022 22:24:56 +09:00
---

This is a list of functions built into MarkTheRipper:

|function|content|
|:----|:----|
|`format`|Format arguments into strings. |
|`relative`|Convert argument paths to relative paths. |
|`lookup`|Draws a metadata dictionary based on the results given by the `argument`. |
|`older`|Enumerates articles older than the argument indicates. |
|`newer`|Enumerates articles newer than the argument indicates. |
|`take`|Limit the number of items that can be enumerated. |
|`add`|Add numeric arguments.|
|`sub`|Subtract numeric arguments.|
|`mul`|Multiply numeric arguments.|
|`div`|Divide numeric arguments.|
|`mod`|Get the remainder of numeric arguments.|
|`embed`|Generate embedded content using [oEmbed protocol](https://oembed.com/) and etc.|
|`card`|Generate card-shaped content using [OGP metadata](https://ogp.me/) and etc.|

## format

Formatting an argument into a string has always been possible without using `format` without any particular problem.
Where this function is particularly useful is when dealing with dates and times.

First, we show how the formatting is determined.
When using the `date` or `generated` keyword in such HTML:

```html
<p>Date: {date}</p>
```

The date and time are output in the following format:

```markdown
---
date: 2022/1/23 12:34:56
lang: en-us
---

(... Body text ...)
```

```html
<p>Date: 1/2/2022 12:34:56 PM +09:00</p>
```

This format varies depending on the language indicated by `lang`.
If `ja-jp` instead of `en-us`, then:

```html
<p>Date: 2022/01/02 12:34:56 +09:00</p>
```

Follow the standard format of the language.
The value of the `timezone` keyword is referenced when formatting the string,
and the timezone-corrected date and time are output.

And if you want to fix the format, use the `format` function as follows:

```html
<p>Date: {format date 'yyyy-MM-dd HH.mm.ss'}</p>
```

```html
<p>Date: 2022-01-02 12.34.56</p>
```

The first argument of the `format` function is an expression indicating the value to be formatted,
and the second argument is a format string.
The format specification string is enclosed in single or double quotes.

As previously mentioned,
"MarkTheRipper is written in .NET, but the user does not need to know about .NET",
only this format string follows the conventions of .NET.

The exact format of the date/time format string is [see this document](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings).

In fact, any value, not just dates and times, can be formatted according to the format.
For example, the `index` in the enumeration is a number, but can be formatted as:

```html
<p>{format item.index 'D3'}</p>
```

The number can be set to fixed three digits.

```html
<p>007</p>
```

The various format strings are also detailed around the .NET documentation listed above.

## relative

As already explained, the `relative` function calculates a path relative to the current article path.

```html
<p>{relative item.path}</p>
```

You can also pass a string as an argument to the function. For example:

```html
<link rel="stylesheet" href="{relative 'github.css'}">
```

This way, you can specify the correct relative path where the stylesheet resides.
The path specified in the argument is relative to the site's root directory, `docs`.
In the above example, the path is relative to `docs/github.css`.

Normally, in such a case, you would specify an absolute path.
However, if you use an absolute path, it will be invalid depending on the deployment environment of your site.
Using the `relative` function improves the portability of the generated content.

## lookup

The `lookup` function searches the metadata dictionary again for keywords
with the same name as the value specified in the argument.
A typical use case is to look up the name of a tag or category in the metadata dictionary:

```html
<p>Tag: {lookup tag}</p>
```

With `{tag}`, the true name of the tag is output,
but with the `lookup` function The output is obtained from the metadata dictionary
with the same keyword value as the tag name.
Thus, if `metadata.json` contains:

```json
{
  "diary": "What happened today"
}
```

You can actually swap the strings that are output.
As we have seen, you can also directly specify a value for a function argument,
so even if you want to output a fixed string, you can use:

```html
<p>Tag: {lookup 'diary'}</p>
```

## older, newer (Article navigation)

You can use `older` and `newer` function to enumerate articles.
For example, to enumerate articles newer than the current article, use the following expression:

```html
{foreach (newer self)}<p>{item.title}</p>{end}
```

where `self` represents the current article (the markdown article currently using this layout) and the individual elements enumerated by `foreach` represent newer articles.
Thus, you can create links by referencing properties and applying the `relative` function as follows:

```html
{foreach (newer self)}<p><a href="{relative item.path}">{item.title}</a></p>{end}
```

Note the position of the parentheses.
In the above, the parentheses are used to specify `self` as an argument to the `newer` function.
Without the parentheses,
you have specified `foreach` with `newer` and `self` as arguments,
which will not work correctly.

The enumerated articles are obtained in date order.
Therefore, MarkTheRipper recommends that blog like post which are date-oriented;
to group them into category such as `blog` and use this function to navigate within those category.

`older` and `newer` function arguments can be expressions that indicate any articles.
An enumerated value of the `entries` property is equivalent.

## take (enumeration operation)

`take` limits the number of possible enumerations of values.
For example, `tag.entries` will enumerate all articles with that tag:

```html
{foreach tags tag}
<h2>{tag}</h2>
{foreach tag.entries}<p>{item.title}</p>{end}
{end}
```

However, you may want to limit this to a few pieces:

```html
{foreach tags tag}
<h2>{tag}</h2>
{foreach (take tag.entries 3)}<p>{item.title}</p>{end}
{end}
```

The `take` function takes two arguments:
The first is the enumeration target and the second is the number of enumerations to limit.
The above expression limits the number to a maximum of 3.

There is a neat little technique using this `take` function and `foreach`.
It is applied in combination with the `newer` function in the previous section:

```html
{foreach (take (newer self) 1)}<a href="{relative item.path}">Newer: {item.title}</a>{end}
```

Limiting the number of articles to be enumerated to one means that if there are zero articles,
they will not be enumerated at all.
In other words, if there are no articles to enumerate,
the `foreach` will not be output, and therefore will not be displayed.
This means that if there are no articles newer than the current article,
the link will not be displayed.

This expression is almost identical to the sample layout.

## add, sub, mul, div, mod (Numerical calculation)

These are functions that perform numerical calculations.
At least one argument is required, and if there are three or more arguments,
the calculation is performed consecutively. For example:

```html
<p>1 + 2 + 3 = {add 1 2 3}</p>
```

Adds all the numbers in the argument.
For complex calculations, use parentheses:

```html
<p>(1 + 2) * 4 = {mul (add 1 2) 4}</p>
```

You can nest any number of parentheses.
Parentheses can be applied and formatted as desired using the `format` function:

```html
<p>1 + 3 = {format (add 1 3) 'D3'}</p>
```

Result:

```html
<p>1 + 3 = 004</p>
```

The argument does not have to be a number,
as long as the string can be regarded as a number:

```html
<p>1 + 2 + 3 = {add 1 '2' 3}</p>
```

It is acceptable to have numbers containing decimals in the arguments.
If so, it will be treated as a calculation with decimals
(called "Floating-point operation"):

```html
<p>1 + 2.1 + 3 = {add 1 2.1 3}</p>
```

Results containing decimals may not always turn out as intended.
It may be better to always use the `format` function to account for such possibilities.
For information on how to specify formats that include decimals, [see here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#fixed-point-format-specifier-f).

Here is a simple example of using the calculation.
In enumeration, `item.index` is a number and starting from 0.
On the other hand, `item.count` is a number that can be enumerated,
but it is not a general notation if you put it in a sequence:

``html
<p>index/count = {item.index}/{item.count}</p>
```

Result:

```html
<p>index/count = 0/3</p>
<p>index/count = 1/3</p>
<p>index/count = 2/3</p>
```

In such a case, you can use `add` function to get:

```html
<p>index/count = {add item.index 1}/{item.count}</p>
```

Would result in a number from 1 to `count`,
which is closer to a natural representation.

Result:

```html
<p>index/count = 1/3</p>
<p>index/count = 2/3</p>
<p>index/count = 3/3</p>
```

## embed (Generates embedded content)

The `embed` (and `card`, described below) functions are special,
powerful functions that generate content by reference to external data,
rather than performing simple calculations.
It is a powerful function that generates content by referencing external data.

Have you ever wanted to embed a YouTube video on your blog?
Or perhaps you have wanted to display card-shaped content to external content,
rather than just a link.

The `embed` and `card` functions make such complex content embedding easy.
For example, you could write the following in your document:

```markdown
## Found a great video!

One day, when I can travel freely, I would like to visit...

{embed https://youtu.be/1La4QzGeaaQ}
```

Then you will see the following:

![embed-sample1-en](Images/embed-sample1-en.png)

This image is not just a thumbnail.
You can actually play the video on the page. Magic! Is it?
This is made possible by using the standard [oEmbed protocol](https://oembed.com/)
to this is achieved by automatically collecting content that should be embedded in HTML.

The argument to the `embed` function is simply the "permalink" of the content,
i.e., the URL that should be shared.
In addition to YouTube, many other well-known content sites support this function, so:

```markdown
## Today's hacking

{embed https://twitter.com/kozy_kekyo/status/1508078650499149827}
```

![embed-sample2-en](Images/embed-sample2-en.png)

Thus, other content, such as Twitter, can be easily embedded. To see which content sites are supported, please [directly refer to oEmbed's json metadata](https://oembed.com/providers.json). You will see that quite a few sites are already supported.

However, one of the most useful sites we know of Amazon,
to our surprise does not support oEmbed!
Therefore, MarkTheRipper specifically recognizes Amazon's product links so that they can be embedded as well:

```markdown
## Learning from Failure

{embed https://amzn.to/3USDXfp}
```

![embed-sample3-en](Images/embed-sample3-en.png)

* This link can be obtained by activating Amazon associates.
  Amazon associates can be activated by anyone with an Amazon account, but we won't go into details here.

Now, a little preparation is required to use this handy function.
Prepare a dedicated layout file `layouts/embed.html` to display this embedded content.
The contents are only as follows:

```html
<div style="max-width:800px;margin:10px;">
    {contentBody}
</div>
```

As in the previous layout explanations,
the `contentBody` will contain the actual oEmbed content to be embedded.
The outer `div` tag determines the area of this embedded content.
In the above, the body is 800px wide with some space around the perimeter.
You may want to adjust this to fit your site's design.

If you need a different layout for each content site,
you can apply a file name like `layouts/embed-YouTube.html` that specifies the name of the oEmbed provider.

By the way, the oEmbed protocol may not contain embedded content.
In such a case, the oEmbed metadata that could be obtained together is used to generate content similar to the `card` function introduced next.

## card (Generate card-shaped content)

The `embed` function directly displays embedded content provided by the content provider.
The `card` function collects content metadata and displays it in a view provided by MarkTheRipper.

Metadata is collected in the following way:

* oEmbed: Using the accompanying metadata (including cases where embedded content was not provided by the `embed` function)
* OGP (Open Graph protocol): Scraping the target page and collecting the OGP metadata contained in the page.
* Amazon: Collect from Amazon associates page.

Usage is exactly the same as the `embed` function:

```markdown
## Found a great video!

One day, when I can travel freely, I would like to visit...

{card https://youtu.be/1La4QzGeaaQ}
```

Then you will see the following :

![card-sample1-en](Images/card-sample1-en.png)

Unlike the `embed` function, it displays the additional information as content and links in a card-shaped format.
Similarly:

```markdown
## Learning from Failure

{card https://amzn.to/3USDXfp}
```

![card-sample3-en](Images/card-sample3-en.png)

Various content can be displayed in card-shaped format.
You can use either the embedded format or the card-shaped format as you like.

Like the `embed` function, the `card` function also requires a dedicated layout file.
The layout file `layouts/card.html` can be adapted to your site based on the following template:

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

This template is completely independent HTML.
If you wish to use it in conjunction with Bootstrap,
please refer to the files included in the sample layouts generated by `mtr init standard` or `mtr init rich`.

Even the `card` function can be customized to be site-specific by naming the file `layouts/card-YouTube.html`.
