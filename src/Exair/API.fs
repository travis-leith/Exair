module API
open MySqlConnector
open System
    type EntityId<'a> = EntityId of int with member this.AsInt = match this with EntityId i -> i
    let i = 1
    let s = nameof(i)

    type DateWithoutTimeBecauseWeirdlyDotnetDoesNotHaveThisConcept = {
        Year:uint
        Month:uint
        Day:uint
    }
    type Person = {
        FullName:String
        PassportNumber:string
        BirthDate:DateWithoutTimeBecauseWeirdlyDotnetDoesNotHaveThisConcept
    }

    type Team = {
        TeamName:string
        TeamMembers:Person list
    }
    
    type Entity<'a> = {
        EntityId:EntityId<'a>
        LastUpdate:DateTime
        VersionNumber:int
        Item:'a
    }

    type Collection<'a> = {
        Connection:MySqlConnection
    }


    //basic crud operations
    let getEntity<'a> : Collection<'a> -> EntityId<'a> -> Option<Entity<'a>> = raise(NotImplementedException())
    // let getEntities<'a> : Collection<'a> -> SearchTerm<'a> list -> Entity<'a> list = raise(NotImplementedException())
    let updateEntity<'a> : Collection<'a> -> Entity<'a> -> Entity<'a> = raise(NotImplementedException())
    let updateEntities<'a> : Collection<'a> -> Entity<'a> list -> Entity<'a> list = raise(NotImplementedException())
    let insertEntity<'a> : Collection<'a> -> 'a -> Entity<'a> = raise(NotImplementedException())
    let insertEntities<'a> : Collection<'a> -> 'a list -> Entity<'a> list = raise(NotImplementedException())
    let deleteEntity<'a> : Collection<'a> -> EntityId<'a> -> unit = raise(NotImplementedException())
    let deleteEntities<'a> : Collection<'a> -> EntityId<'a> list -> unit = raise(NotImplementedException())

    // let createCollection<'a> : SearchKey<'a> list -> ForeignKey<'a> list -> Collection<'a> = raise(NotImplementedException())
    let getCollection<'a> : unit -> Collection<'a> = raise(NotImplementedException())