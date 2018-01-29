module Client

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.PowerPack.Fetch

open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Elements.Form
open Fulma.Components
open Fulma.BulmaClasses
open Fulma.BulmaClasses.Bulma
open Fable.Import.React

type Model =
  { Posts: Post list
    SelectedPost: Post option
    PostContent: string option }

type Msg =
| Init of Result<Post list, exn>
| ChangeCurrentPost of Post
| DisplayContent of Result<string, exn>

let init () = 
  let model =
    { Posts = List.Empty
      SelectedPost = None
      PostContent = None }
  let cmd' = Cmd.ofAsync
  let cmd =
    Cmd.ofPromise 
      (fetchAs<Post list> "/api/posts")
      [] 
      (Ok >> Init) 
      (Error >> Init)
  model, cmd

let fetchAsString (url: string) (init: RequestProperties list) : JS.Promise<string> =
  fetch url init
  |> Promise.bind (fun fetched -> fetched.text())

let update msg (model : Model) =
  let model' =
    match msg with
    | Init (Ok posts) -> { model with Posts = posts }
    | ChangeCurrentPost post -> { model with SelectedPost = Some post
                                             PostContent = None }
    | DisplayContent (Ok content) -> { model with PostContent = Some content }
    | _ -> { model with SelectedPost = None
                        PostContent = None }
  let cmd' =
    match msg with
    | ChangeCurrentPost post ->
      Cmd.ofPromise
        (fetchAsString (sprintf "/api/post_content/%i" post.Id))
        [] 
        (Ok >> DisplayContent) 
        (Error >> DisplayContent)
    | _ -> Cmd.none
  model', cmd'

let show = function
| Some x -> string x
| None -> "Loading..."

let menuItem label isActive onclick =
    li []
       [ a [ classList [ Bulma.Menu.State.IsActive, isActive ]
             OnClick onclick ]
       [ str label ] ]

let createPostMenuItem post selectPost =
  function
  | Some currentPost -> menuItem post.Title (currentPost.Id = post.Id) (fun _ -> selectPost post)
  | None -> menuItem post.Title false (fun _ -> selectPost post)

type [<Pojo>] InnerHtml =
  { __html: string }

let setInnerHtml (html: string) =
  DangerouslySetInnerHTML { __html = html }

let showPost post content =
  match (post, content) with
  | (Some p, Some c) -> [ Heading.h2 [] [str p.Title]
                          Content.content [ Content.props [ setInnerHtml c ] ] [ ] ]
  | (Some p, None) -> [ Heading.h2 [] [str p.Title]
                        div [classList [ "loader", true ] ] [] ]
  | _ -> [ Content.content [] [ str "Select a post to display on the left" ] ]

let view (model: Model) dispatch =
  let selectPost p = dispatch (ChangeCurrentPost p)
  div []
    [ Navbar.navbar [ Navbar.customClass "is-primary" ]
        [ Navbar.item_div [ ]
            [ Heading.h2 [ ]
                [ str "Fathom" ] ] ]
      Section.section [ ]
        [ Container.container [Container.isFluid]
            [ Columns.columns [] [
                Column.column [Column.Width.isOneQuarter]
                  [ Menu.menu []
                      [ Menu.label [] [ str (sprintf "Posts (%i)" model.Posts.Length) ]
                        Menu.list []
                          [ for post in model.Posts do
                            yield (createPostMenuItem post selectPost model.SelectedPost) ] ] ]
                Column.column []
                  (showPost model.SelectedPost model.PostContent) ] ] ] ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
