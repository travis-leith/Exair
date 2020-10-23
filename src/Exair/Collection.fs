namespace Exair
open MySqlConnector

type CollectionDefinition = {
    TableName:string
    Keys:Key list
}

module private CollectionHelpers =
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

module Collection =
    let OfType<'a> = {
        TableName = sprintf "Main_%s" typeof<'a>.Name
        Keys = []
    }

    let WithKey key def = {def with Keys = key::def.Keys}

    let Create (conn:MySqlConnection) tableDefinition =
        let sql = CollectionHelpers.getCreateTableSql tableDefinition
        use cmd = new MySqlCommand(sql, conn)
        cmd.ExecuteNonQuery() |> ignore