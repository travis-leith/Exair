module IntegrationTests
open Expecto
open Expecto.Flip
open Exair
open MySqlConnector

type private Student = {
    Name:string
    StudentId:int
    ShoolHouse:string
}

type private SchoolHouse = {
    HouseName:string
    FlagColor: uint8 * uint8 * uint8
    Motto:string
}


let usageTests run =
    [
        testCase "Create a new collection" (fun _ ->
            let shcoolHouseDef =
                Collection.OfType<SchoolHouse>
                |> Collection.WithUniqueKey (Key.Create (fun s -> s.HouseName))

            run (fun conn ->
                shcoolHouseDef
                |> Collection.Create conn
                |> Expect.equal "No error is raised" ()
            )
        )
    ]

[<Tests>]
let databaseUsageTests =
    usageTests (fun test ->
        let connString = "Server=127.0.0.1;Port=3306;Database=json_testing;Uid=exairtest;Pwd=exairtest;"
        use conn = new MySqlConnection(connString)
        conn.Open()

        test conn
    ) |> testList "Integration Tests" |> testSequenced
