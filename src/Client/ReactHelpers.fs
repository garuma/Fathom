module Helpers.React

open Fable.Core
open Fable.Helpers.React.Props

type [<Pojo>] InnerHtml =
  { __html: string }

let setInnerHtml (html: string) =
  DangerouslySetInnerHTML { __html = html }