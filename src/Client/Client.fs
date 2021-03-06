module Client

open Fable.Core
open Fable.Import

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.PowerPack.Fetch

open Shared
open Helpers.React

open Fulma.Layouts
open Fulma.Elements
open Fulma.Components
open Fulma.BulmaClasses
open Fulma.BulmaClasses.Bulma

type Model =
  { Feeds: FeedGroup
    SelectedPost: Post option
    PostContent: string option }

type Msg =
| Init of Result<FeedGroup, exn>
| ChangeCurrentPost of Post
| DisplayContent of Result<string, exn>

let init () = 
  let model =
    { Feeds = { Feeds = List.Empty }
      SelectedPost = None
      PostContent = None }
  let cmd' = Cmd.ofAsync
  let cmd =
    Cmd.ofPromise 
      (fetchAs<FeedGroup> "/api/posts")
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
    | Init (Ok feedGroup) -> { model with Feeds = feedGroup }
    | ChangeCurrentPost post -> { model with SelectedPost = Some post
                                             PostContent = None }
    | DisplayContent (Ok content) -> { model with PostContent = Some content }
    | _ -> { model with SelectedPost = None
                        PostContent = None }
  let cmd' =
    match msg with
    | ChangeCurrentPost post ->
      Cmd.ofPromise
        (fetchAsString (sprintf "/api/post_content/%s/%s" post.FeedId post.Id))
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

let createPostMenuItem (post: Post) selectPost (selectedPost: Post option) =
  match selectedPost with
  | Some currentPost -> menuItem post.Title (currentPost.Id = post.Id) (fun _ -> selectPost post)
  | None -> menuItem post.Title false (fun _ -> selectPost post)

let showPost (post: Post option) content =
  match (post, content) with
  | (Some p, Some c) -> [ Heading.h2 [] [str p.Title]
                          Content.content [ Content.props [ setInnerHtml c ] ] [ ] ]
  | (Some p, None) -> [ Heading.h2 [] [str p.Title]
                        div [classList [ "loader", true ] ] [] ]
  | _ -> [ Content.content [] [ str "Select a post to display on the left" ] ]

let view (model: Model) dispatch =
  let selectPost p = dispatch (ChangeCurrentPost p)
  div [Style [ Height "100%"; ]]
    [ Container.container [Container.isFluid
                           Container.props [Style [Height "100%"; MarginTop "2em"; MarginBottom "1em" ]] ]
          [ Columns.columns [Columns.props [Style [Height "100%"; OverflowY "hidden" ]]] [
              Column.column [Column.Width.isOneQuarter
                             Column.props [Style [ OverflowY "auto" ]]]
                [ h1 [Class "main title"] [ str "Fathom" ]
                  h2 [Class "is-hidden-touch main subtitle"] [ str "An F# feed reader" ]
                  Menu.menu []
                    [ for feed in model.Feeds.Feeds do
                      yield Menu.label [] [
                        Level.level [] [
                          Level.left [] [
                            Level.item [] [
                              p [] [ str feed.FeedName ]
                            ]
                          ]
                          Level.right [] [
                            Level.item [] [
                              Tag.tag [Tag.customClass "is-rounded"] [ str (feed.Posts.Length.ToString()) ]
                            ]
                          ]
                        ]
                      ]
                      yield Menu.list []
                        [ for post in feed.Posts do
                          yield (createPostMenuItem post selectPost model.SelectedPost) ] ] ]
              Column.column [Column.props [Style [ OverflowY "auto" ]]]
                (showPost model.SelectedPost model.PostContent) ] ] ]

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
