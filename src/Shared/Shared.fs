namespace Shared

open System

type AtomId = string

type Post =
  { Id: AtomId
    FeedId: AtomId
    Title: string
    Author: string
    Date: DateTime }


