namespace Shared

open System

type AtomId = string
type UserId = string

type Post =
  { Id: AtomId
    FeedId: AtomId
    Title: string
    Authors: string list
    Date: DateTime }

type Feed =
  { Id: AtomId
    FeedName: String
    Posts: Post list
  }

type FeedGroup =
  { Feeds: Feed list }