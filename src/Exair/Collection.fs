namespace Exair
open MySqlConnector

type CollectionDefinition = {
    TableName:string
    SearchKeys: Key list
    UniqueKeys:Key list
    ForeignKeys:(Key * string) list
}

module private CollectionHelpers =
    let getCreateTableSql tableDefinition =
        let keySql isUnique key =
            let uniqueNess = if isUnique then "unique " else ""
            [
                sprintf "%s %s as (json_value(json_info, '%s')) stored" key.Name key.KeyType key.Path // does not work for multi valued keys
                sprintf "check (%s is not null)" key.Name
                sprintf "%skey (%s)" uniqueNess key.Name
            ]

        let keysSql =
            [
                yield! tableDefinition.SearchKeys |> List.collect (keySql false)
                yield! tableDefinition.UniqueKeys |> List.collect (keySql true)
            ]

        let createClauses =
            [
                "entity_id int not null auto_increment primary key" //automatically assigned by DB
                "last_update timestamp not null" //automatically assigned by DB
                "version_number int not null default 1" //gets incremented by a trigger
                "json_info json not null"
                "check (json_valid(json_info))"
                yield! keysSql
            ] |> String.concat ","  |> sprintf "create or replace table %s(%s)" tableDefinition.TableName

        let triggerSql =
            [
                sprintf "create or replace trigger %s_versioning before update on %s" tableDefinition.TableName tableDefinition.TableName
                "for each row"
                "set new.version_number = old.version_number + 1"
            ] |> String.concat " "

        sprintf "%s;%s;" createClauses triggerSql

module Collection =
    let OfType<'a> = {
        TableName = sprintf "Main_%s" typeof<'a>.Name
        //PrimaryKey = None
        SearchKeys = []
        UniqueKeys = []
        ForeignKeys = []
    }

    let WithSearchKey key def =
        {def with SearchKeys = key::def.SearchKeys}

    let WithUniqueKey key def =
        {def with UniqueKeys = key::def.UniqueKeys}

    let WithForeignKey<'a> key def =
        let typeName = typeof<'a>.Name
        {def with ForeignKeys = (key, typeName)::def.ForeignKeys}

    let Create (conn:MySqlConnection) tableDefinition =
        let sql = CollectionHelpers.getCreateTableSql tableDefinition
        use cmd = new MySqlCommand(sql, conn)
        cmd.ExecuteNonQuery() |> ignore