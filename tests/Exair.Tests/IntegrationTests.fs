module IntegrationTests
open Expecto
open Expecto.Flip
open Exair
open Exair.Types
open MySqlConnector

type private Student = {
    Name:string
    StudentId:int
    ShoolHouse:string
}

type private SchoolHouse = {
    HouseName:string
    FlagColor: string
    UnbiasedDescription:string
}

let databaseUsageTests() =
    try
        let connString = "Server=127.0.0.1;Port=3306;Uid=exairtest;Pwd=exairtest;"
        use conn = new MySqlConnection(connString)
        conn.Open()

        let dbName = "school"
        let schoolDb = Database.Create conn dbName

        let schoolHouseCollection =
            Collection.OfType<SchoolHouse> schoolDb
            |> Collection.WithUniqueKey (Key.Create (fun s -> s.HouseName))
            |> Collection.Create conn

        let stubbsHouse = { HouseName = "Stubbs"; FlagColor = "Yellow(more like Gold!)"; UnbiasedDescription = "Most awesome" }
        let stubbsId =
            stubbsHouse
            |> Entity.Insert schoolHouseCollection

        let otherHouses = [
            { HouseName = "Bullimore"; FlagColor = "Blue"; UnbiasedDescription = "Jerks" }
            { HouseName = "Haysom"; FlagColor = "Red"; UnbiasedDescription = "Losers" }
            { HouseName = "Evans"; FlagColor = "Green"; UnbiasedDescription = "Weirdos" }
        ]
            
        let otherIds = otherHouses |> Entity.InsertList schoolHouseCollection

        let selectedStubbs = Entity.Get schoolHouseCollection stubbsId
        let selectedOthers = Entity.GetList schoolHouseCollection otherIds

        testList "Basic CRUD" [
            test "Insert single value" {
                stubbsId.AsInt |> Expect.equal "First Id is 1" 1UL
            }

            test "Insert multiple values" {
                otherIds |> List.map (fun x -> x.AsInt) |> Expect.equal "Ids are sequenced" [2UL; 3UL; 4UL]
            }

            test "Select a single value" {
                selectedStubbs.Item |> Expect.equal "Should be same as insertred" stubbsHouse
            }

            test "Select multiple values" {
                selectedOthers |> List.sortBy (fun e -> e.EntityId) |> List.map (fun e -> e.Item)
                |> Expect.equal "Should be same as inserted" otherHouses
            }
        ]
    with
    |ex ->
        testCase "in case of some error" (fun _ ->
            raise ex
        )

[<Tests>]
let integrationTests =
    testList "IntegrationTests" [
        databaseUsageTests()
    ]