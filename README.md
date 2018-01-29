# Fathom

Prototype of an (incomplete) atom feed reader in F# with the [SAFE stack](https://safe-stack.github.io/).

![screenshot](https://neteril.org/~jeremie/fatom-screenshot-shadow3.png)

Pieces used (at least those I touched):

 - [Giraffe](https://github.com/giraffe-fsharp/Giraffe)
 - [Fable](https://github.com/fable-compiler/Fable)
 - [Fulma](https://github.com/MangelMaxime/Fulma)

## Building

Use the `build.cmd` or `build.sh` script.

⚠️ Those will pull around 1GB of dependencies (I know, I know)

## Interesting gotchas

From a first-run experience point of view, here are the troubles I ran into and the workarounds for them

 - JSON interop between Giraffe and Fable is tricky because Fable expects a certain kind of JSON to be transmitted to reconstruct F# records which Giraffe default `json` modifier doesn't produce. Adding configuration option to Giraffe [is being tracked](https://github.com/giraffe-fsharp/Giraffe/issues/178). Workaround is to use [a custom serialize function](https://github.com/garuma/Fatom/blob/master/src/Server/FableJson.fs).
 - Type providers don't work on .NET Core because of the hosted compiler, you have to use .NET or Mono `fsc` for it to work properly. [Summary issue with workaround there](https://github.com/Microsoft/visualfsharp/issues/3303).
 - For someone (like me) who has no idea about React, injecting custom HTML snippet into the page is done via the `dangerouslySetInnerHTML` attribute, [a nice function wrapper for this attribute is available there](https://github.com/fable-compiler/fable-helpers/blob/master/src/Fable.Helpers.WebGenerator.fs#L49-L53).
