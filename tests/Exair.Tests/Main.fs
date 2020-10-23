module Exair.Tests
open Expecto
open API
open MySqlConnector

// type User = {
//     UserName:string
//     Password:string
//     Salt:string
//     Groups:string list
// }

[<EntryPoint>]
let main argv =
    // let connString = "Server=127.0.0.1;Port=3306;Database=json_testing;Uid=exairtest;Pwd=exairtest;"
    // use conn = new MySqlConnection(connString)
    // conn.Open()

    // let def = defineCollection<User>
    // let sql = getCreateTableSql def
    // use cmd = new MySqlCommand(sql, conn)
    // cmd.ExecuteNonQuery() |> ignore

    // printfn "sql: %s" sql
    Tests.runTestsInAssemblyWithCLIArgs [] argv
