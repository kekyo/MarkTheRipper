---
title: Tags and Categories
lang: en-us
date: 10/26/2022 22:24:55 +09:00
---

"Tags" and "categories" are necessary when organizing documents such as blogs and articles.
MarkTheRipper can of course use these to classify articles.

## Aggregate tags

Once you understand how to use repetition, you are as good as done with tags and categories.
MarkTheRipper automatically aggregates all the tags and categorizations of your content.
Tags can be referenced by the following special keywords:

|Keywords|Note|
|:----|:----|
|`tagList`|Aggregate list of all tags.|
|`rootCategory`|A category that is the root. (It is no classification)|

First, let's make a list of tags:

```html
<ul>
  {foreach tagList}
  <li>{item}</li>
  {end}
</ul>
```

Result:

```html
<ul>
  <li>foo</li>
  <li>bar</li>
  <li>baz</li>
       :
</ul>
```

In the previous section we repeated the use of `tags` defined for the markdown file, here we use `tagList`.
The difference is that we are not dealing with a single markdown file, but with the aggregated tags of all markdown files processed by MarkTheRipper.

In other words, you can use `tagList` to add menu items and link lists by tags.
How do I add a link to each tag entry?
Tags alone do not tell us the set of contents associated with a tag,
but in fact tags can be enumerated using `foreach`:

```html
{foreach tagList tag}
<h1>{tag}</h1>
{foreach tag.entries entry}
<h2><a href="{relative entry.path}">{entry.title}</a></h2>
{end}
{end}
```

Note that we specify the bound name to make it easier to understand what we are trying to enumerate.

Enumerating the `entries` property gives access to information about
the corresponding markdown group.
Using the property `path`, you get the path to the file corresponding to the content,
and use `title` to get its title (the `title` described in the markdown's header).

* `path` yields the path to the converted HTML file, not the path to the markdown.

By the way, this path is relative to the output directory.
If you embed the path in HTML, it must be relative to the directory where the HTML file resides.
To do this calculation, use MarkTheRipper's built-in function keyword `relative`:

```html
<h2><a href="{relative entry.path}">{entry.title}</a></h2>
```

Using `relative` to calculate the path will work correctly
how the HTML output by MarkTheRipper is deployed on any server.
This will be safer than using the reference path for hard-coded URLs.

## Aggregate categories

Categories can be referenced by the following special keywords:

|keywords|content|
|:----|:----|
|`category`|a hierarchical list of categories. Describe in the header part of each markdown|
|`rootCategory`|The root (unclassified) category|

The critical difference between tags and categories is that tags are defined in parallel,
while categories are defined with hierarchy. For example:

````
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
````

In the above example, `foobarbaz1.md` belongs to the category `foo/bar/baz`.
And `blog1.md` does not belong to any category,
it is supposed to belong to implicit hidden `(root)` category inside MarkTheRipper.
It is the `rootCategory` keyword.

For tags we defined it using the `tags` keyword, but for categories we use the keyword `category`.
The definition corresponding to `foobarbaz1.md` above is:

```markdown.
---
title: Hello MarkTheRipper
category: foo,bar,baz
---

(... Body ...)
```

Specifies the hierarchy as a list. Note that unlike tags, this list represents a hierarchy.
CMSs and site generators often refer to such hierarchies as "breadcrumb" lists.

By the way, MarkTheRipper can determine the category from the directory name by simply placing
the content in a categorized subdirectory,
without having to specify the category with the `category` keyword.
Therefore, to categorize content by category, you only need to place it in categorized subdirectories.

Now that we have grasped the basic structure of categories,
let's actually write the layout.
First, enumerate the root category:

```html
<h1>{rootCategory.name}</h1>
<ul>
  {foreach rootCategory.entries entry entry}
  <li>{entry.path}</li>
  {end}
</ul>
```

Result:

```html
<h1>(root)</h1>
<ul>
  <li>blog1.html</li>
  <li>blog2.html</li>
</ul>
```

Since `rootCategory` represents the root category, its property `name` is `(root)`.
If this name is not appropriate for display,
you can replace it using recursive keyword search expression,
or you can write it directly since it is root in this example.

Then, like the tags, you can pull the header information for each markdown
from each of the elements enumerated in the `entries`.

Here, `path` is used to output the path to the content, but you can use `title` to output the title.
If you use `relative item.path`, you can get the path relative to the current content,
which can be used as the URL of the link to realize the link.

To enumerate categories, use the `children` property:

```html
<h1>{rootCategory.name}</h1>
{foreach rootCategory.children child1}
<h2>{child1.name}</h2>
{foreach child1.children child2}
<h3>{child2.name}</h3>
{end}
{end}
```

If we nest the enumerations repeately, we can enumerate all deep category structures.
Unfortunately, it is not possible to dynamically enumerate the category structure,
i.e., automatic recursively enumerate even the descendant categories that exist.
This is by design because MarkTheRipper does not have the ability to define any functions and recursive functions.
(Such a request is probably only for outputting a site-wide structure list, as we did not see the need for such a request.)

At the end of the category operation is an example of outputting breadcrumb list.
It is very simple:

```html
<ul>
  {foreach category.breadcrumbs}
  <li>{item.name}</li>
  {end}
</ul>
```

The `breadcrumbs` property returns a value that allows you to enumerate the categories leading to the target category,
starting from the root.
(However, if the target category is root, the root category is included; otherwise, it is not included.)

The individual elements enumerated are the same as for the categories described so far.
In the above example, the `name` property outputs the name of the category.
