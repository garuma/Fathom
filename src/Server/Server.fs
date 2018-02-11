open System
open System.IO
open System.Xml
open System.Text
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json

open Giraffe
open Giraffe.Serialization.Json

open Shared
open Microsoft.AspNetCore.WebUtilities
open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Table

open System.ServiceModel.Syndication

[<Literal>]
let ConnectionString = "..."

//type AtomFeed = XmlProvider<"http://blog.neteril.org/feed.xml", Encoding="utf-8">
type XmlFeedUrl =
  | Atom of string
  | Rss2 of string
type Database = AzureTypeProvider<ConnectionString>

type DatabaseEntry =
  { Title: string
    Content: string }

let DefaultFeeds = [|
  Atom "http://blog.neteril.org/feed.xml";
  Rss2 "https://blog.xamarin.com/feed/";
  Rss2 "http://tirania.org/blog/miguel.rss2"
|]

let clientPath =
  [ "client"
    Path.Combine("..","Client") ]
  |> List.find System.IO.Directory.Exists
  |> Path.GetFullPath
let port = 8085us

let makeId (id: string) =
  WebEncoders.Base64UrlEncode (Encoding.UTF8.GetBytes(id))

let makeFeedId (feed: SyndicationFeed) =
  makeId (if isNull feed.Id then feed.Title.Text else feed.Id)

let downloadFeed (xmlFeedUrl: XmlFeedUrl) =
  let createXmlReader (url: string) = XmlReader.Create(url)
  let reader =
    match xmlFeedUrl with
    | Atom url -> createXmlReader url
    | Rss2 url -> createXmlReader url
  Task.Run(fun () -> SyndicationFeed.Load(reader))

let formatAuthors (persons: seq<SyndicationPerson>) =
  [ for person in persons do yield person.Name ]

let gatherPosts (feed: SyndicationFeed) =
  let feedId = makeFeedId feed
  let posts =
    [ for entry in feed.Items do
      yield { Id = makeId entry.Id
              FeedId = feedId
              Title = entry.Title.Text
              Authors = (formatAuthors entry.Authors)
              Date = entry.PublishDate.DateTime }
    ]
  { Id = feedId;
    FeedName = feed.Title.Text;
    Posts = posts }

let getItemContent (item: SyndicationItem) =
  if isNotNull item.Content then
    (item.Content :?> TextSyndicationContent).Text
  else if isNotNull item.Summary then
    item.Summary.Text
  else
    "No content"


let feedDatabase (feeds: SyndicationFeed[]) =
  task {
    let entries = seq {
      for feed in feeds do
        for entry in feed.Items do
          yield (Partition (makeFeedId feed), Row (makeId entry.Id), { Title = entry.Title.Text; Content = (getItemContent entry) })
    }
    let! result = Async.StartAsTask(Database.Tables.FathomApp.InsertAsync(entries, TableInsertMode.Upsert))
    printfn "%A" result
  }

let gatherFeeds feedUrls =
  Task.WhenAll(Array.map downloadFeed feedUrls)

let getPosts (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! feeds = gatherFeeds DefaultFeeds
    do! feedDatabase feeds
    let parsedFeeds = Array.map gatherPosts feeds
    let feedGroup = { Feeds = Array.toList parsedFeeds }
    return! json feedGroup next ctx
  }

let getPostContentString feedId id =
  task {
    let! entity = Async.StartAsTask(Database.Tables.FathomApp.GetAsync(Row id, Partition feedId))
    let content =
      match entity with
      | Some e -> e.Content
      | None -> sprintf "No content for post with id %s" id
    return content
  }

let getPostContent (feedId, id) (next : HttpFunc) (ctx : HttpContext) =
  task {
    let! content = getPostContentString feedId id
    return! htmlString content next ctx
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

let configureServices (services : IServiceCollection) =
    // First register all default Giraffe dependencies
    services.AddGiraffe() |> ignore

    // Configure JsonSerializer to use Fable.JsonConverter
    let fableJsonSettings = JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())

    services.AddSingleton<IJsonSerializer>(
        NewtonsoftJsonSerializer(fableJsonSettings)) |> ignore

WebHost
  .CreateDefaultBuilder()
  .UseWebRoot(clientPath)
  .UseContentRoot(clientPath)
  .Configure(Action<IApplicationBuilder> configureApp)
  .ConfigureServices(configureServices)
  .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
  .Build()
  .Run()