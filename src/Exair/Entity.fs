namespace Exair
open System

type EntityId<'a> = EntityId of int with member this.AsInt = match this with EntityId i -> i

type Entity<'a> = {
    EntityId:EntityId<'a>
    LastUpdate:DateTime
    VersionNumber:int
    Item:'a
}