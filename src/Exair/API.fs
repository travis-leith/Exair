namespace Exair
open MySqlConnector
open Exair.Types
open Thoth.Json.Net
open System

[<RequireQualifiedAccess>]
module Database =
    module private Helpers =
        let getCreateSql dbName =
            let dbCreation =
                sprintf "create or replace database %s" dbName
            let collectionsMetadata =
                sprintf "create or replace table %s.collections_metadata(collection_name varchar(255) not null primary key, collection_details json not null)" dbName
            sprintf "%s;%s;" dbCreation collectionsMetadata

    let Create (conn:MySqlConnection) dbName =
        let sql = Helpers.getCreateSql dbName
        use cmd = new MySqlCommand(sql, conn)
        cmd.ExecuteNonQuery() |> ignore

        {
            DbName = dbName
        }

[<RequireQualifiedAccess>]
module Collection =
    module private Helpers =
        let getCreateTableSql collection =
            let keySql isUnique key =
                let uniqueNess = if isUnique then "unique " else ""
                [
                    sprintf "%s %s as (json_value(json_info, '%s')) stored" key.Name key.KeyDbType key.Path // does not work for multi valued keys
                    sprintf "check (%s is not null)" key.Name
                    sprintf "%skey (%s)" uniqueNess key.Name
                ]

            let keysSql =
                [
                    yield! collection.SearchKeys |> List.collect (keySql false)
                    yield! collection.UniqueKeys |> List.collect (keySql true)
                ]

            let createClauses =
                [
                    "entity_id bigint unsigned not null auto_increment primary key" //automatically assigned by DB
                    "last_update timestamp not null default current_timestamp() on update current_timestamp()" //automatically assigned by DB
                    "version_number int not null default 1" //gets incremented by a trigger
                    "json_info json not null"
                    "check (json_valid(json_info))"
                    yield! keysSql
                ] |> String.concat ","  |> sprintf "create or replace table %s.%s(%s)" collection.Database.DbName collection.TableName

            let triggerSql =
                [
                    sprintf "create or replace trigger %s.%s_versioning before update on %s.%s" collection.Database.DbName collection.TableName collection.Database.DbName collection.TableName
                    "for each row"
                    "set new.version_number = old.version_number + 1"
                ] |> String.concat " "

            let extraCoders =
                Extra.empty
                |> Extra.withCustom TypeCoder.encoder TypeCoder.decoder

            let insertMetaDataSql =
                let json = Encode.Auto.toString(4, collection, extra = extraCoders)
                sprintf "insert into %s.collections_metadata values ('%s','%s')" collection.Database.DbName collection.TableName json

            sprintf "%s;%s;%s;" createClauses triggerSql insertMetaDataSql

    let OfType<'a> database : Collection<'a> = {
        CollectionType = typeof<'a>
        Database = database
        TableName = sprintf "Main_%s" typeof<'a>.Name
        SearchKeys = []
        UniqueKeys = []
        ForeignKeys = []
        Connection = None
    }

    let WithSearchKey key def =
        {def with SearchKeys = key::def.SearchKeys}

    let WithUniqueKey key def =
        {def with UniqueKeys = key::def.UniqueKeys}

    let WithForeignKey<'a> key def =
        let typeName = typeof<'a>.Name
        {def with ForeignKeys = (key, typeName)::def.ForeignKeys}

    let Create (conn:MySqlConnection) collection =
        //let collection = OfType<'a> database
        let sql = Helpers.getCreateTableSql collection
        use cmd = new MySqlCommand(sql, conn)
        cmd.ExecuteNonQuery() |> ignore
        {collection with Connection = Some conn}

module Entity =
    module private Helpers =
        let withConnection f = function
            |Some (conn:MySqlConnection) when conn.State = System.Data.ConnectionState.Open -> f conn
            |_ -> failwith "Collection does not have an open connection"

        let insertSql (collection:Collection<'a>) (item :'a) =
            Encode.Auto.toString(2, item)
            |> sprintf "insert into %s.%s(json_info) values ('%s'); select last_insert_id();" collection.Database.DbName collection.TableName

        let insertListSql (collection:Collection<'a>) (items :'a list) =
            items |> List.map (fun x -> Encode.Auto.toString(2, x) |> sprintf "('%s')")
            |> String.concat ","
            |> sprintf "insert into %s.%s(json_info) values %s; select last_insert_id();" collection.Database.DbName collection.TableName

        let getSql (collection:Collection<'a>) (entityId:EntityId<'a>) =
            sprintf "select entity_id, unix_timestamp(last_update) last_update, version_number, json_info from %s.%s where entity_id = %i;" collection.Database.DbName collection.TableName entityId.AsInt

        let getListSql (collection:Collection<'a>) (entityIds:EntityId<'a> list) =
            entityIds |> List.map (fun x -> x.AsInt |> sprintf "%i")
            |> String.concat ","
            |> sprintf "select entity_id, unix_timestamp(last_update) last_update, version_number, json_info from %s.%s where entity_id in (%s);" collection.Database.DbName collection.TableName

        let constructEntities<'a> (rdr:MySqlDataReader) : Entity<'a> list =
            [
                while rdr.Read() do
                    yield
                        {
                            EntityId = rdr.GetUInt64("entity_id") |> EntityId
                            LastUpdate = rdr.GetInt64("last_update") |> DateTimeOffset.FromUnixTimeSeconds
                            VersionNumber = rdr.GetInt32("version_number")
                            Item = rdr.GetString("json_info") |> Decode.Auto.unsafeFromString
                        }
            ]

    let Insert collection (item:'a) : EntityId<'a> =
        collection.Connection |> Helpers.withConnection (fun conn ->
            let sql = Helpers.insertSql collection item
            use cmd = new MySqlCommand(sql, conn)
            cmd.ExecuteScalar() :?> uint64 |> EntityId
        )

    let InsertList collection (items:'a list) : EntityId<'a> list =
        collection.Connection |> Helpers.withConnection (fun conn ->
            let sql = Helpers.insertListSql collection items
            use cmd = new MySqlCommand(sql, conn)
            let firstId = cmd.ExecuteScalar() :?> uint64 |> EntityId
            items |> List.mapi (fun i _ -> firstId.AsInt + (uint64 i) |> EntityId)
        )

    let Get collection (entityId:EntityId<'a>) : Entity<'a> =
        collection.Connection |> Helpers.withConnection (fun conn ->
            let sql = Helpers.getSql collection entityId
            use cmd = new MySqlCommand(sql, conn)
            use rdr = cmd.ExecuteReader()
            Helpers.constructEntities rdr |> Seq.exactlyOne
        )

    let GetList collection (entityIds:EntityId<'a> list) : Entity<'a> list =
        collection.Connection |> Helpers.withConnection (fun conn ->
            let sql = Helpers.getListSql collection entityIds
            use cmd = new MySqlCommand(sql, conn)
            use rdr = cmd.ExecuteReader()
            Helpers.constructEntities rdr
        )