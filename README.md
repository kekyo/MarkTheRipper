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

[![Japanese language](Images/Japanese.256.png)](https://github.com/kekyo/MarkTheRipper/blob/main/README_ja.md)

## What is this?

TODO: Eventually this document will be converted by MarkTheRipper itself.

MarkTheRipper is a very simple and fast static site generator
that allows you to write content in markdown.
The main intended use is for blog sites,
but we have eliminated the need for complex structures and tools anyway,
as if you were writing an article on GitHub Gist likely.

If you already have .NET 6.0 installation, you can install it simply:

```bash
dotnet tool install -g MarkTheRipper
```

Then at first time, you will need to run:

```bash
$ mtr new mininum
```

Will generate a sample files under your current directory as follows
(Don't be afraid! It's only TWO FILES with a few lines of content
and almost no extra definitions!)

* `contents` directory: `index.md`,
  It is a content (post) file written by markdown.
* `resources` directory: `template-page.html`,
  When the site is generated, the markdown is converted to HTML and inserted into this template file.

That's it! Just to be sure, let's show you what's inside:

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

If you look at the content, you can probably guess what happens:
MarkTheRipper simply converts the keywords and body into HTML and inserts it into the template.
Therefore when customizing a template,
common HTML/JavaScript techniques can be applied as is,
and there are no restrictions.

(MarkTheRipper is written in .NET, but the user does not need to know about .NET)

Let's generate the site as is. Generating a site is very easy:

```bash
$ mtr
````

If your directory structure is the same as the sample, just run ``mtr`` to generate the site.
Site generation is multi-threaded and multi-asynchronous I/O driven,
so it is fast even with a large amount of content.
By default, the output is under the `docs` directory.
In this example, the `contents/index.md` file is converted and placed in the `docs/index.html` file.

You will then immediately see a preview in your default browser:

![minimum image](Images/minimum.png)

Site generation will delete all files in the `docs` directory and generate them again each time.
If you manage the entire directory with Git,
you can commit the site including the `docs` directory.
Then you can check the differences of the actual generated files.
You can also push it straight to `github.io` and easily publish your site!

There are no restrictions on the file names of markdown files or the subdirectories in which they are placed.
If there are files with the extension `.md` under the `contents` directory,
it does not matter what kind of subdirectories they are placed in,
under what kind of file names, or even if there are many of them.
All `.md` files will be converted to `.html` files, keeping the structure of the subdirectories.

Files with non `.md` extensions will simply be copied to the same location.
Additional files, such as pictures for example,
can be placed as you wish to manage them,
and you can write markdowns with relative paths to point to them.

----

## A more practical sample

The samples generated by `mtr new minimum` are too simple (minimum is not a bad thing!),
but in case you want to see some more customized examples,
we have included two more samples as standard.

Two more examples are built in:

```bash
$ mtr new standard
$ mtr new rich
````

You can specify the sample `standard` or `rich`.
The following features are available:

* Add a navigation menu ([bootstrap.js 5.0](https://getbootstrap.jp/)).
* Fancy code blocks:
  * `standard`: Looks like GitHub's code block design (but does not have syntax highlighting).
  * `rich`: Syntax highlighting by [prism.js](https://prismjs.com/).

![standard image](Images/standard.png)

![rich image](Images/rich.png)

No need to worry. These samples have also been implemented
with the utmost care to keep the template code to a simplest.
They are easy to understand and even HTML beginners
can start customizing with these samples.

----

## Template details

MarkTheRipper templates are very simple, yet flexible and applicable enough for your needs.
The templates provide all keyword substitutions by referencing a "Metadata dictionary".

Here is an example of a template substitution.

### Keyword Substitution

The simplest example is a simple keyword substitution. Suppose you define the following template:

```html
<title>{title}</title>
```

This will replace the keyword `title` in the metadata dictionary with the corresponding value.
The value of `title` is defined at the beginning of the corresponding markdown document:

```markdown
---
title: Hello MarkTheRipper!
---

(... Body ...)
```

If you have tried other site generators, you may know how to add these special "header lines" to your markdown to insert a title, date, tags and etc.
MarkTheRipper also follows this convention syntactically, but is more flexible.
For example, the following example produces exactly the same result:

```html
<title>{foobar}</title>
```

```markdown
---
foobar: Hello MarkTheRipper!
---

(... Body ...)
```

Do you somehow see how you can make use of metadata dictionaries?
In other words, MarkTheRipper can treat any set of "key-value" pairs you write in the markdown header as a metadata dictionary,
and you can reflect any number of them on the template.

Arbitrary keywords can be substituted anywhere on the template,
allowing for example the following applications:

```html
<link rel="stylesheet" href="{stylesheet}.css">
```

```markdown
---
title: Hello MarkTheRipper!
stylesheet: darcula
---

(... Body ...)
````

Perhaps this feature alone will solve most of the problems.

### Special keywords and fallbacks

There are several special but potentially important keywords in this metadata dictionary.
They are listed below:

|keywords|content|
|:----|:----|
|`now`|Date and time when the site was generated.|
|`template`|The name of the template to apply.|
|`lang`|Locale (`en-us`, `ja-jp`, etc.)|
|`date`|Date of the post|

These keywords can be overridden by writing them in the markdown header.
It may not make sense to override `now`, but just know that MarkTheRipper does not treat metadata dictionary definitions specially.

You may be wondering what the default values of `lang` and `template` are.
Metadata dictionaries can be placed in `resources/metadata.json`,
which is the base definition for site generation.
(It does not have to be there. In fact, it is not present in the minimum sample.)
For example, the following definition:

```json
{
  "title": "(Draft)",
  "author": "Mark the Ripper",
  "template": "page",
  "lang": "en-us"
}
```

This reveals something interesting. The value of the `title` keyword is "(Draft)".
For example, consider the following template:

```html
<meta name="author" content="{author}" />
<title>{title}</title>
```

If you specify `title` in the markdown, that title will be inserted,
otherwise the title "(Draft)" will be inserted.
Similarly, if `author` is specified, the name of author will be inserted,
otherwise "Mark the Ripper" will be inserted.

If you are thinking of using this for a blog, you may not want to put your name in the header of the markdown,
since most of your posts will be written by yourself.
However, the title will of course be different for each post.
In such cases, you can use the "fallback" feature of the metadata dictionary.

And as for the `lang` and `template` fallback:

* Only if `template` is not found in the fallback, the template name `page` is used.
* Only if `lang` is not found in the fallback, the system default language is applied.

The template name may need some supplementation.
The template name is used to identify the template file from which the conversion is being made.
For example, if the template name is `page`, the file `resources/template-page.html` will be applied. If:

```markdown
---
title: Hello MarkTheRipper!
template: fancy
---
```

then `resources/template-fancy.html` will be used.

The `date` represents the date and time of the article and is treated like an ordinary keyword,
but if it is not defined in the markdown header,
the date and time of generation will be inserted into the markdown header automatically.

You may feel that `lang` is simply one of the ordinary keywords.
This is explained in the next section.

### Format parameters

TODO:

### Recursive keyword search

TODO:

### Iterators and nesting

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
