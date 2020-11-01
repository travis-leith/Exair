module Exair.MariaDb
open MySqlConnector
open Thoth.Json.Net
open System

let private selectFields = "entity_id, unix_timestamp(last_update) last_update, version_number, json_info"

[<RequireQualifiedAccess>]
module private Database =
    let createOrReplaceSql dbName =
        let dbCreation =
            sprintf "create or replace database %s" dbName
        let collectionsMetadata =
            sprintf "create or replace table %s.collections_metadata(collection_name varchar(255) not null primary key, collection_details json not null)" dbName
        sprintf "%s;%s;" dbCreation collectionsMetadata

[<RequireQualifiedAccess>]
module private Collection =
    let createOrReplaceSql collection =
        let keySql isUnique key =
            let uniqueNess = if isUnique then "unique " else ""
            [
                sprintf "%s %s as (json_value(json_info, '%s')) stored" key.Name key.KeyDbType key.JsonPath.AsString // does not work for multi valued keys
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
            ] |> String.concat ","  |> sprintf "create or replace table %s.%s(%s)" collection.Database.AsString collection.TableName

        let triggerSql =
            [
                sprintf "create or replace trigger %s.%s_versioning before update on %s.%s" collection.Database.AsString collection.TableName collection.Database.AsString collection.TableName
                "for each row"
                "set new.version_number = old.version_number + 1"
            ] |> String.concat " "

        let extraCoders =
            Extra.empty
            |> Extra.withCustom TypeCoder.encoder TypeCoder.decoder

        let insertMetaDataSql =
            let json = Encode.Auto.toString(4, collection, extra = extraCoders)
            sprintf "insert into %s.collections_metadata values ('%s','%s')" collection.Database.AsString collection.TableName json

        sprintf "%s;%s;%s;" createClauses triggerSql insertMetaDataSql

[<RequireQualifiedAccess>]
module private EntityInsert =
    let insertSql (q:InsertQuery<'a>) =
        let valuesSql =
            q.Values
            |> List.map (fun x -> Encode.Auto.toString(2, x) |> sprintf "('%s')")
            |> String.concat ","

        let query =
                sprintf "insert into %s.%s(json_info) values %s;select last_insert_id();"
                    q.Collection.Database.AsString q.Collection.TableName valuesSql

        query

[<RequireQualifiedAccess>]
module private EntitySelect =
    let evalBinary = function
        | And -> "AND"
        | Or -> "OR"

    //let evalOrderDirection = function
    //    | Asc -> "ASC"
    //    | Desc -> "DESC"

    let rec evalWhere (w:Where) paramIdSeed =
        match w with
        |Empty -> "", [], paramIdSeed
        |Key (key, comp) ->
            let singleValueHelper op o =
                let paramName = sprintf "@p%i" paramIdSeed
                let sql = sprintf "%s %s %s" key.Name op paramName
                sql, [paramName, o], paramIdSeed + 1

            let multiValueHelper op os =
                let paramsSql, paramList, newSeed =
                    (([], [], paramIdSeed), os) ||> List.fold (fun (sql, parList, seed) o ->
                        let paramName = sprintf "@p%i" seed
                        paramName::sql, (paramName, o)::parList, paramIdSeed + 1
                    )
                let sql = paramsSql |> String.concat "," |> sprintf "%s %s (%s)" key.Name op
                sql, paramList, newSeed

            match comp with
            |Eq o -> singleValueHelper "=" o
            |Ne o -> singleValueHelper "<>" o
            |Gt o -> singleValueHelper ">" o
            |Lt o -> singleValueHelper "<" o
            |Ge o -> singleValueHelper ">=" o
            |Le o -> singleValueHelper "<=" o
            |In os -> multiValueHelper "IN" os
            |NotIn os -> multiValueHelper "NOT IN" os
        |Binary(w1, op, w2) ->
            let sql1, parList1, newSeed1 = evalWhere w1 paramIdSeed
            if sql1 = "" then
                evalWhere w2 paramIdSeed
            else
                let sql2, parList2, newSeed2 = evalWhere w1 newSeed1
                let sql = sprintf "(%s %s %s)" sql1 (evalBinary op) sql2
                let parList = parList1 @ parList2
                sql, parList, newSeed2
        | Unary (Not, w) ->
            match evalWhere w paramIdSeed with
            | "", _, _ -> "", [], paramIdSeed
            | sql, parList, newSeed -> sprintf "NOT (%s)" sql, parList, newSeed
                
            
    let selectSql (q:SelectQuery<'a>) =
        //let aggregates = q.Aggregates |> evalAggregates
        //let fieldNames =
        //    fields
        //    |> List.map (replaceFieldWithAggregate aggregates)
        //    |> String.concat ", "
        // distinct
        //let distinct = if q.Distinct then "DISTINCT " else ""
        // basic query

        //let sb = StringBuilder(sprintf "SELECT * FROM %s.%s" q.Collection.Database.AsString q.Collection.TableName)
        // joins
        //let joins = evalJoins q.Joins
        //if joins.Length > 0 then sb.Append joins |> ignore
        // where
        //let where = evalWhere q.Where
        //if where.Length > 0 then sb.Append (sprintf " WHERE %s" where) |> ignore
        //// group by
        ////let groupBy = evalGroupBy q.GroupBy
        ////if groupBy.Length > 0 then sb.Append (sprintf " GROUP BY %s" groupBy) |> ignore
        ////// order by
        ////let orderBy = evalOrderBy q.OrderBy
        ////if orderBy.Length > 0 then sb.Append (sprintf " ORDER BY %s" orderBy) |> ignore
        ////// pagination
        ////let pagination = evalPagination q.Pagination
        ////if pagination.Length > 0 then sb.Append (sprintf " %s" pagination) |> ignore
        //sb.ToString()
        let whereSql, parList, _ = evalWhere q.Where 0
        let sql =
            [
                sprintf "select %s from %s.%s" selectFields q.Collection.Database.AsString q.Collection.TableName
                sprintf "where %s" whereSql
            ] |> String.concat " "
        sql, parList

[<RequireQualifiedAccess>]
module private Entity =
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

[<RequireQualifiedAccess>]
module private EntityGet =
    let getSql (q:GetQuery<'a>) =
        let whereDetail =
            match q.EntityIds with
            |[x] -> sprintf "= %i" x.AsInt
            |xs -> xs |> List.map (fun x -> sprintf "%i" x.AsInt) |> String.concat "," |> sprintf "in (%s)"
        [
            sprintf "select %s from %s.%s" selectFields q.Collection.Database.AsString q.Collection.TableName
            sprintf "where entity_id %s" whereDetail
        ] |> String.concat " "

type MySqlConnection with
    member conn.CreateOrReplaceDatabase (Database dbName, ?tran:MySqlTransaction) =
        let sql = Database.createOrReplaceSql dbName
        use cmd = new MySqlCommand(sql, conn, tran |> Option.toObj)
        cmd.ExecuteNonQuery() |> ignore

    member conn.CreateOrReplaceCollection (collection, ?tran:MySqlTransaction) =
        let sql = Collection.createOrReplaceSql collection
        use cmd = new MySqlCommand(sql, conn, tran |> Option.toObj)
        cmd.ExecuteNonQuery() |> ignore

    member conn.Insert<'a> (q:InsertQuery<'a>, ?tran:MySqlTransaction) : EntityId<'a> list =
        let sql = EntityInsert.insertSql q
        use cmd = new MySqlCommand(sql, conn, tran |> Option.toObj)
        let firstId = cmd.ExecuteScalar() :?> uint64
        q.Values |> List.mapi (fun i _ -> firstId + (uint64 i) |> EntityId)

    member conn.Select<'a> (q:SelectQuery<'a>, ?tran:MySqlTransaction) : Entity<'a> list =
        let sql, parList = EntitySelect.selectSql q
        use cmd = new MySqlCommand(sql, conn, tran |> Option.toObj)
        parList |> List.iter (fun (parName, o) -> cmd.Parameters.AddWithValue(parName, o) |> ignore)
        use rdr = cmd.ExecuteReader()
        Entity.constructEntities rdr

    member conn.Get<'a> (q:GetQuery<'a>, ?tran:MySqlTransaction) : Entity<'a> list =
        let sql = EntityGet.getSql q
        use cmd = new MySqlCommand(sql, conn, tran |> Option.toObj)
        use rdr = cmd.ExecuteReader()
        Entity.constructEntities rdr