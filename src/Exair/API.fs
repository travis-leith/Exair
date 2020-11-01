namespace Exair
open MySqlConnector
open Exair.Types
open Thoth.Json.Net
open System





//module Entity =
//    module private Helpers =
//        let withConnection f = function
//            |Some (conn:MySqlConnection) when conn.State = System.Data.ConnectionState.Open -> f conn
//            |_ -> failwith "Collection does not have an open connection"

//        let insertSql (collection:Collection<'a>) (item :'a) =
//            Encode.Auto.toString(2, item)
//            |> sprintf "insert into %s.%s(json_info) values ('%s'); select last_insert_id();" collection.Database.DbName collection.TableName

//        let insertListSql (collection:Collection<'a>) (items :'a list) =
//            items |> List.map (fun x -> Encode.Auto.toString(2, x) |> sprintf "('%s')")
//            |> String.concat ","
//            |> sprintf "insert into %s.%s(json_info) values %s; select last_insert_id();" collection.Database.DbName collection.TableName

//        let getSql (collection:Collection<'a>) (entityId:EntityId<'a>) =
//            sprintf "select entity_id, unix_timestamp(last_update) last_update, version_number, json_info from %s.%s where entity_id = %i;" collection.Database.DbName collection.TableName entityId.AsInt

//        let getListSql (collection:Collection<'a>) (entityIds:EntityId<'a> list) =
//            entityIds |> List.map (fun x -> x.AsInt |> sprintf "%i")
//            |> String.concat ","
//            |> sprintf "select entity_id, unix_timestamp(last_update) last_update, version_number, json_info from %s.%s where entity_id in (%s);" collection.Database.DbName collection.TableName

//        let constructEntities<'a> (rdr:MySqlDataReader) : Entity<'a> list =
//            [
//                while rdr.Read() do
//                    yield
//                        {
//                            EntityId = rdr.GetUInt64("entity_id") |> EntityId
//                            LastUpdate = rdr.GetInt64("last_update") |> DateTimeOffset.FromUnixTimeSeconds
//                            VersionNumber = rdr.GetInt32("version_number")
//                            Item = rdr.GetString("json_info") |> Decode.Auto.unsafeFromString
//                        }
//            ]

//    let Insert collection (item:'a) : EntityId<'a> =
//        collection.Connection |> Helpers.withConnection (fun conn ->
//            let sql = Helpers.insertSql collection item
//            use cmd = new MySqlCommand(sql, conn)
//            cmd.ExecuteScalar() :?> uint64 |> EntityId
//        )

//    let InsertList collection (items:'a list) : EntityId<'a> list =
//        collection.Connection |> Helpers.withConnection (fun conn ->
//            let sql = Helpers.insertListSql collection items
//            use cmd = new MySqlCommand(sql, conn)
//            let firstId = cmd.ExecuteScalar() :?> uint64 |> EntityId
//            items |> List.mapi (fun i _ -> firstId.AsInt + (uint64 i) |> EntityId)
//        )

//    let Get collection (entityId:EntityId<'a>) : Entity<'a> =
//        collection.Connection |> Helpers.withConnection (fun conn ->
//            let sql = Helpers.getSql collection entityId
//            use cmd = new MySqlCommand(sql, conn)
//            use rdr = cmd.ExecuteReader()
//            Helpers.constructEntities rdr |> Seq.exactlyOne
//        )

//    let GetList collection (entityIds:EntityId<'a> list) : Entity<'a> list =
//        collection.Connection |> Helpers.withConnection (fun conn ->
//            let sql = Helpers.getListSql collection entityIds
//            use cmd = new MySqlCommand(sql, conn)
//            use rdr = cmd.ExecuteReader()
//            Helpers.constructEntities rdr
//        )