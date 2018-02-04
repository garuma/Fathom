open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http

open Giraffe
open FSharp.Data
open FableJson

open Shared

type AtomFeed = XmlProvider<"http://blog.neteril.org/feed.xml">

let clientPath =
  [ "client"
    Path.Combine("..","Client") ]
  |> List.find System.IO.Directory.Exists
  |> Path.GetFullPath
let port = 8085us

let gatherPosts (feed: AtomFeed.Feed) =
  [ for entry in feed.Entries do
    yield { Id = entry.Id.GetHashCode()
            Title = entry.Title.Value
            Author = "Jérémie Laval"
            Date = entry.Published
    } ]

let getPosts (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! feed = AtomFeed.AsyncLoad "http://blog.neteril.org/feed.xml"
    return! serialize (gatherPosts feed) next ctx
  }

let getPostContentString id =
  task {
    let! feed = AtomFeed.AsyncLoad "http://blog.neteril.org/feed.xml"
    let post = Array.tryFind (fun (e: AtomFeed.Entry) -> e.Id.GetHashCode() = id) feed.Entries
    let content =
      match post with
      | Some p -> p.Content.Value
      | None -> sprintf "No content for post with id %i" id
    return content
  }

let getPostContent id (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! content = getPostContentString id
    return! html content next ctx
  }

let webApp : HttpHandler =
  choose [
    GET >=>
      choose [
        route  "/api/posts" >=> getPosts
        routef "/api/post_content/%i" getPostContent
      ]
  ]

let configureApp  (app : IApplicationBuilder) =
  app.UseStaticFiles()
     .UseGiraffe webApp

WebHost
  .CreateDefaultBuilder()
  .UseWebRoot(clientPath)
  .UseContentRoot(clientPath)
  .Configure(Action<IApplicationBuilder> configureApp)
  .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
  .Build()
  .Run()