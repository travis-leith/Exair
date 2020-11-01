module IntegrationTests
open Expecto
open Expecto.Flip
open Exair
open Exair.MariaDb
open MySqlConnector

type Student = {
    Name:string
    StudentId:int
    ShoolHouse:string
}

type SchoolHouse = {
    HouseName:string
    FlagColor: string
    UnbiasedDescription:string
    TrophyCount:System.Int32
}

[<RequireQualifiedAccess>]
module SchoolHouseKeys =
    let houseName = Key.Create (fun s -> s.HouseName)
    //let trophyCount = Key.Create (fun s -> s.TrophyCount)

let databaseUsageTests() =
    try
        let connString = "Server=127.0.0.1;Port=3306;Uid=exairtest;Pwd=exairtest;"
        use conn = new MySqlConnection(connString)
        conn.Open()

        let schoolDb = "school" |> Database
        schoolDb |> conn.CreateOrReplaceDatabase

        
        let schoolHouseCollection =
            Collection.OfType<SchoolHouse> schoolDb
            |> Collection.WithUniqueKey SchoolHouseKeys.houseName

        schoolHouseCollection |> conn.CreateOrReplaceCollection

        let stubbsHouse = { HouseName = "Stubbs"; FlagColor = "Yellow (more like Gold!)"; UnbiasedDescription = "Most awesome"; TrophyCount = 100}
        let stubbsId =
            insertInto schoolHouseCollection {
                value stubbsHouse
            } |> conn.Insert |> List.exactlyOne
            

        let otherHouses = [
            { HouseName = "Bullimore"; FlagColor = "Blue"; UnbiasedDescription = "Jerks"; TrophyCount = 95}
            { HouseName = "Haysom"; FlagColor = "Red"; UnbiasedDescription = "Losers"; TrophyCount = 3}
            { HouseName = "Evans"; FlagColor = "Green"; UnbiasedDescription = "Weirdos"; TrophyCount = 32}
        ]
            
        let otherIds =
            insertInto schoolHouseCollection {
                values otherHouses
            } |> conn.Insert

        let fetchedStubbs =
            getFrom schoolHouseCollection {
                entityId stubbsId
            } |> conn.Get |> List.exactlyOne

        let fetchedOthers =
            getFrom schoolHouseCollection {
                entityIds otherIds
            } |> conn.Get

        let selectedBullimore =
            selectFrom schoolHouseCollection {
                where (eq SchoolHouseKeys.houseName "Bullimore")
            } |> conn.Select |> List.exactlyOne

        let selectedOthers =
            selectFrom schoolHouseCollection {
                where (isIn SchoolHouseKeys.houseName ["Evans"; "Haysom"])
            } |> conn.Select

        testList "Basic CRUD" [
            test "Insert single value" {
                stubbsId.AsInt |> Expect.equal "" 1UL
            }

            test "Insert multiple values" {
                otherIds |> List.map (fun x -> x.AsInt) |> Expect.equal "" [2UL; 3UL; 4UL]
            }

            test "Get a single value" {
                fetchedStubbs.Item |> Expect.equal "" stubbsHouse
            }

            test "Get multiple values" {
                fetchedOthers |> List.sortBy (fun e -> e.EntityId) |> List.map (fun e -> e.Item)
                |> Expect.equal "Should be same as inserted" otherHouses
            }

            test "Select with equality" {
                selectedBullimore.Item.UnbiasedDescription |> Expect.equal "" "Jerks"
            }

            test "Select in list" {
                selectedOthers |> List.sumBy (fun x -> x.EntityId.AsInt) |> Expect.equal "" 7UL
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