module API
open MySqlConnector
open System
    type EntityId<'a> = EntityId of int with member this.AsInt = match this with EntityId i -> i

    type Entity<'a> = {
        EntityId:EntityId<'a>
        LastUpdate:DateTime
        VersionNumber:int
        Item:'a
    }

    type Collection<'a> = {
        Connection:MySqlConnection
    }

    type CollectionDefinition = {
        TableName:String
    }

    let defineCollection<'a> = {
        TableName = sprintf "Main_%s" typeof<'a>.Name
    }

    let getCreateTableSql tableDefinition =

    // let keySql (keyName:KeyName, keyDef) =

    //   let keyFunctionSql =

    //     match keyDef.KeyFunction with

    //     |Simple -> sprintf "key (%s)" keyName.AsString

    //     |Unique -> sprintf "uinque key (%s)" keyName.AsString

    //   [

    //     sprintf "%s int as (json_value(json_info, '%s.%s')) stored" keyName.AsString keyDef.KeyPath keyName.AsString

    //     sprintf "check (%s is not null)" keyName.AsString

    //     keyFunctionSql

    //   ]

    // let keysSql = keys |> Map.toList |> List.collect keySql

        let createClauses =
            [
                "entity_id int not null auto_increment primary key" //automatically assigned by DB
                "last_update timestamp not null" //automatically assigned by DB
                "version_number int not null default 1" //gets incremented by a trigger
                "json_info json not null"
                "check (json_valid(json_info))"
            ] |> String.concat ","  |> sprintf "create or replace table %s(%s)" tableDefinition.TableName

        let triggerSql =
            [
                sprintf "create or replace trigger %s_versioning before update on %s" tableDefinition.TableName tableDefinition.TableName
                "for each row"
                "set new.version_number = old.version_number + 1"
            ] |> String.concat " "

        sprintf "%s;%s;" createClauses triggerSql

    let createTable (conn:MySqlConnection) tableDefinition =
        let sql = getCreateTableSql tableDefinition
        use cmd = new MySqlCommand(sql, conn)
        cmd.ExecuteNonQuery() |> ignore

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