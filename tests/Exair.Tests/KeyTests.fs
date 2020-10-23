module KeyTests

open Expecto
open Expecto.Flip
open Exair

type A = {
    AInt:int
    AString:string
    AInts:int list
    AStrings:string list
}

type B = {
    BAs:A list
}

type C = {
    CBs:B list
}

[<Tests>]
let tests =
    testList "Keys" [
        testList "Path Tests" [
            testCase "level 1 single int" (fun _ ->
                    let x = Key.Create(fun (a:A) -> a.AInt)
                    x.Path |> Expect.equal "should be correct path" "$.AInt"
                )
            testCase "level 1 single string" (fun _ ->
                    let x = Key.Create(fun (a:A) -> a.AString)
                    x.Path |> Expect.equal "should be correct path" "$.AString"
                )
            testCase "level 1 multiple int" (fun _ ->
                    let x = Key.Create(fun (a:A) -> a.AInts)
                    x.Path |> Expect.equal "should be correct path" "$.AInts[*]"
                )
            testCase "level 1 multiple string" (fun _ ->
                    let x = Key.Create(fun (a:A) -> a.AStrings)
                    x.Path |> Expect.equal "should be correct path" "$.AStrings[*]"
                )
        
            testCase "level 2 single int" (fun _ ->
                    let x = Key.Create(fun (b:B) -> b.BAs |> List.map (fun a -> a.AInt))
                    x.Path |> Expect.equal "should be correct path" "$.BAs[*].AInt"
                )
            testCase "level 2 single string" (fun _ ->
                    let x = Key.Create(fun (b:B) -> b.BAs |> List.map (fun a -> a.AString))
                    x.Path |> Expect.equal "should be correct path" "$.BAs[*].AString"
                )
            testCase "level 2 multiple int" (fun _ ->
                    let x = Key.Create(fun (b:B) -> b.BAs |> List.collect (fun a -> a.AInts))
                    x.Path |> Expect.equal "should be correct path" "$.BAs[*].AInts[*]"
                )
            testCase "level 2 multiple string" (fun _ ->
                    let x = Key.Create(fun (b:B) -> b.BAs |> List.collect (fun a -> a.AStrings))
                    x.Path |> Expect.equal "should be correct path" "$.BAs[*].AStrings[*]"
                )

            testCase "level 3 multiple string" (fun _ ->
                    let x = Key.Create(fun (c:C) -> c.CBs |> List.collect(fun b -> b.BAs |> List.collect (fun a -> a.AStrings))) 
                    x.Path |> Expect.equal "should be correct path" "$.CBs[*].BAs[*].AStrings[*]"
                )
        ]
    ]
    

//let clientTests setup =
//    [
//        test "test1" {
//            setup (fun client store ->
//                ()
//            )
//        }
//        test "test2" {
//            setup (fun client store ->
//                ()
//            )
//        }
//        // other tests
//    ]

//let clientMemoryTests =
//    clientTests (fun test ->
//        let client = 1
//        let store = 1
//        test client store
//    )
//    |> testList "client memory tests"

//let clientIntegrationTests =
//    clientTests (fun test ->
//            // setup code
//            try
//                let client = 1 //realTestClient()
//                let store = 2 //realTestStore()
//                test client store
//            finally
//                // teardown code
//                ()
//        )
//    |> testList "client integration tests"