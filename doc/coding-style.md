# C# Coding Style

The general rule we follow is "use Visual Studio defaults".
To enforce this, we make use of an .editorconfig file, and [CodeMaid](https://marketplace.visualstudio.com/items?itemName=SteveCadwallader.CodeMaidVS2022) with some rules exported.

## Specific rules

1. We use [Allman style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) braces, where each brace begins on a new line.
  > Note: a single line statement block can go without braces but the block must be properly indented on its own line and it must not be nested in other statement blocks that use braces (See issue [381](https://github.com/dotnet/corefx/issues/381) for examples).
1. Logging needs to be conditional to avoid interpolated string resolution.
  > As a result of superfluous logging, we allow for avoiding braces in just this case to avoid bloating a method with too many lines.
1. We use four spaces of indentation (no tabs).
1. Variable casing and style:
  - Use `_camelCase` for internal and private fields and use `readonly` where possible.
  - Prefix instance fields with `_`, static fields with `s_` and thread static fields with `t_`.
  - When used on static fields, `readonly` should come after `static` (i.e. `static readonly` not `readonly static`).
1. We avoid `this.` unless absolutely necessary.
1. We always specify the visibility, even if it's the default (i.e.
   `private string _foo` not `string _foo`). Visibility should be the first modifier (i.e. 
   `public abstract` not `abstract public`).
1. Namespace imports should be specified at the top of the file, *outside* of
   `namespace` declarations and should be sorted alphabetically.
1. Avoid more than one empty line at any time. For example, do not have two blank lines between members of a type.
1. Avoid spurious free spaces.
   For example avoid `if (someVar == 0)...`, where the dots mark the spurious free spaces.
   Consider enabling "View White Space (Ctrl+E, S)" if using Visual Studio, to aid detection.
1. If a file happens to differ in style from these guidelines (e.g. private members are named `m_member`
   rather than `_member`), the existing style in that file takes precedence, unless one chooses to convert the entire file.
1. We only use `var` when it's obvious what the variable type is (i.e. `var stream = new FileStream(...)` not `var stream = OpenStandardInput()`).
  - Alternatively, in C# 9.0, one can use `Dictionary<string, string> pairs = new();`
1. We use language keywords instead of BCL types (i.e. `int` instead of `Int32`, etc.) for both type references as well as method calls (i.e. `int.Parse` instead of `Int32.Parse`).
  > Note: see issue [391](https://github.com/dotnet/corefx/issues/391) for examples.
1. We use PascalCasing to name all our constant local variables and fields.
  > Note: the only exception is for interop code where the constant value should exactly match the name and value of the code you are calling via interop.
1. We use `nameof(...)` instead of `"..."` whenever possible and relevant.
1. Order of members in a class definition:
  - Fields (in order: constants, static readonly, static, instance).
  - Constructors and factory methods (in order: public, protected, internal, private).
  - Properties (in order: public, protected, internal).
  - Methods (in order: public, protected, internal, private).
1. When including non-ASCII characters in the source code use Unicode escape sequences (\uXXXX) instead of literal characters. Literal non-ASCII characters occasionally get garbled by a tool or editor.
1. Models properties default instances:
  - Collections should be given a default instance, but if null or empty not be serialized.
    > Note: while track1, they should also be public settable.
  - Submodels should not be given a default instance.
1. Solution folders are used for organizing large numbers of files. We generally aim to keep namespaces to a minimum: the one defined for the project.
1. Avoid long lines that would require scrolling on a standard monitor size when being compared in a side-by-side view in a PR.
  - Usual options:
    - Break up parameters, one line per parameter.
    - Break up fluent-style calls.
  - Example:
    ```csharp
    await theObject
        .SomeMethodAsync(
            param1,
            param2,
            param3)
        .ConfigureAwait(false);
    ```
1. Order of tags in documentation comments:
  - `<summary>` - Describe a type or a member of a type
  - `<remarks>` - Describe additional information about a type
  - `<param>` - Describe a parameter for a method or constructor
  - `<returns>` - Describe the return value of a method
  - `<exception>` - Identifies the exceptions a method can throw
  - `<example>` - Indicate an example

## Examples

ObservableLinkedList`1.cs:

```C#
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace System.Collections.Generic
{
    public partial class ObservableLinkedList<T> : INotifyCollectionChanged, INotifyPropertyChanged
    {
        private ObservableLinkedListNode<T> _head;
        private int _count;

        public ObservableLinkedList(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (T item in items)
            {
                AddLast(item);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => _count;

        public ObservableLinkedListNode AddLast(T value) 
        {
            var newNode = new LinkedListNode<T>(this, value);

            InsertNodeBefore(_head, node);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void InsertNodeBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
           ...
        }
        
        ...
    }
}
```

ObservableLinkedList`1.ObservableLinkedListNode.cs:

```C#
using System;

namespace System.Collections.Generics
{
    partial class ObservableLinkedList<T>
    {
        public class ObservableLinkedListNode
        {
            private readonly ObservableLinkedList<T> _parent;
            private readonly T _value;

            internal ObservableLinkedListNode(ObservableLinkedList<T> parent, T value)
            {
                Debug.Assert(parent != null);

                _parent = parent;
                _value = value;
            }
            
            public T Value => _value;
        }

        ...
    }
}
```
