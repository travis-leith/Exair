module CollectionTests

open Expecto
open Expecto.Flip
open Exair

type Student = {
    Name:string
    StudentId:int
}

[<Tests>]
let definitionTests =
    testList "Collections" [
        testList "Definition Tests" [
            testCase "Single Key Collection" (fun _ ->
                let collection =
                    Collection.OfType<Student>
                    |> Collection.WithKey (Key.Create (fun s -> s.StudentId))

                collection.Keys.Length |> Expect.equal "Should be 1" 1
            )

            testCase "Multiple Key Collection" (fun _ ->
                let collection =
                    Collection.OfType<Student>
                    |> Collection.WithKey (Key.Create (fun s -> s.StudentId))
                    |> Collection.WithKey (Key.Create (fun s -> s.Name))

                collection.Keys.Length |> Expect.equal "Should be 2" 2
            )
        ]
        testList "Integration Tests" [
        ]
    ]
    

