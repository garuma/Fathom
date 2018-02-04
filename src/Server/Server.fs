open System
open System.IO

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http

open Giraffe
open FSharp.Data
open FableJson

open Shared
open Microsoft.AspNetCore.WebUtilities
open System.Text
open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Table

[<Literal>]
let ConnectionString = "..."

type AtomFeed = XmlProvider<"http://blog.neteril.org/feed.xml", Encoding="utf-8">
type Database = AzureTypeProvider<ConnectionString>

type DatabaseEntry =
  { Title: string
    Content: string }

let clientPath =
  [ "client"
    Path.Combine("..","Client") ]
  |> List.find System.IO.Directory.Exists
  |> Path.GetFullPath
let port = 8085us

let makeId (id: string) =
  WebEncoders.Base64UrlEncode (Encoding.UTF8.GetBytes(id))

let gatherPosts (feed: AtomFeed.Feed) =
  let feedId = makeId feed.Id
  [ for entry in feed.Entries do
    yield { Id = makeId entry.Id
            FeedId = feedId
            Title = entry.Title.Value
            Author = "Jérémie Laval"
            Date = entry.Published
    } ]

let feedDatabase (feed: AtomFeed.Feed) =
  async {
    let entries = seq {
      for entry in feed.Entries do
        yield (Partition (makeId feed.Id), Row (makeId entry.Id), { Title = entry.Title.Value; Content = entry.Content.Value })
    }
    let! result = Database.Tables.FathomApp.InsertAsync(entries, TableInsertMode.Upsert)
    printfn "%A" result
  }

let getPosts (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! feed = AtomFeed.AsyncLoad "http://blog.neteril.org/feed.xml"
    do! feedDatabase feed
    let posts = gatherPosts feed
    return! serialize posts next ctx
  }

let getPostContentString feedId id =
  task {
    let! entity = Database.Tables.FathomApp.GetAsync(Row id, Partition feedId)
    let content =
      match entity with
      | Some e -> e.Content
      | None -> sprintf "No content for post with id %s" id
    return content
  }

let getPostContent (feedId, id) (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! content = getPostContentString feedId id
    return! html content next ctx
  }

let webApp : HttpHandler =
  choose [
    GET >=>
      choose [
        route  "/api/posts" >=> getPosts
        routef "/api/post_content/%s/%s" getPostContent
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