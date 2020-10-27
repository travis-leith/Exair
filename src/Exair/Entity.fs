namespace Exair.Types
open System

type EntityId<'a> = EntityId of uint64 with member this.AsInt = match this with EntityId i -> i

type Entity<'a> = {
    EntityId:EntityId<'a>
    LastUpdate:DateTimeOffset
    VersionNumber:int
    Item:'a
}