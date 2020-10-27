module CollectionTests

open Expecto
open Expecto.Flip
open Exair
open Exair.Types

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


//[<Tests>]
//let definitionTests =
//    testList "Collections" [
//        testList "Definition Tests" [
//            testCase "Search Key Exists" (fun _ ->
//                let collection =
//                    Collection.OfType<Student> db
//                    |> Collection.WithSearchKey (Key.Create (fun s -> s.StudentId))

//                collection.SearchKeys.Length |> Expect.equal "Should be 1" 1
//            )

//            testCase "Unique Key Exists" (fun _ ->
//                let collection =
//                    Collection.OfType<Student> db
//                    |> Collection.WithUniqueKey (Key.Create (fun s -> s.StudentId))

//                collection.UniqueKeys.Length |> Expect.equal "Should be 1" 1
//            )

//            testCase "Foreign Key Exists" (fun _ ->
//                let collection =
//                    Collection.OfType<Student>
//                    |> Collection.WithForeignKey<SchoolHouse> (Key.Create (fun s -> s.HouseName))

//                collection.ForeignKeys.Length |> Expect.equal "Should be 1" 1
//            )

//            testCase "Foreign Key Type Name" (fun _ ->
//                let collection =
//                    Collection.OfType<Student>
//                    |> Collection.WithForeignKey<SchoolHouse> (Key.Create (fun s -> s.HouseName))

//                collection.ForeignKeys.Head |> snd |> Expect.equal "Should be named correctly" typeof<SchoolHouse>.Name
//            )
//        ]
//    ]
    

