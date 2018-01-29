namespace Shared

open System

type PostId = int

type Post =
  { Id: PostId
    Title: string
    Author: string
    Date: DateTime }


