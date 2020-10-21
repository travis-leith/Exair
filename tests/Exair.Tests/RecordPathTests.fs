module RecordPathTests

open Expecto
open Expecto.Flip
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
  testList "RecordPath" [
    testCase "level 1 single int" (fun _ ->
            let x = RecordPath.Create(fun (a:A) -> a.AInt)
            x.Path |> Expect.equal "should be correct path" "$.AInt"
        )
    testCase "level 1 single string" (fun _ ->
            let x = RecordPath.Create(fun (a:A) -> a.AString)
            x.Path |> Expect.equal "should be correct path" "$.AString"
        )
    testCase "level 1 multiple int" (fun _ ->
            let x = RecordPath.Create(fun (a:A) -> a.AInts)
            x.Path |> Expect.equal "should be correct path" "$.AInts[*]"
        )
    testCase "level 1 multiple string" (fun _ ->
            let x = RecordPath.Create(fun (a:A) -> a.AStrings)
            x.Path |> Expect.equal "should be correct path" "$.AStrings[*]"
        )
    
    testCase "level 2 single int" (fun _ ->
            let x = RecordPath.Create(fun (b:B) -> b.BAs |> List.map (fun a -> a.AInt))
            x.Path |> Expect.equal "should be correct path" "$.BAs[*].AInt"
        )
    testCase "level 2 single string" (fun _ ->
            let x = RecordPath.Create(fun (b:B) -> b.BAs |> List.map (fun a -> a.AString))
            x.Path |> Expect.equal "should be correct path" "$.BAs[*].AString"
        )
    testCase "level 2 multiple int" (fun _ ->
            let x = RecordPath.Create(fun (b:B) -> b.BAs |> List.collect (fun a -> a.AInts))
            x.Path |> Expect.equal "should be correct path" "$.BAs[*].AInts[*]"
        )
    testCase "level 2 multiple string" (fun _ ->
            let x = RecordPath.Create(fun (b:B) -> b.BAs |> List.collect (fun a -> a.AStrings))
            x.Path |> Expect.equal "should be correct path" "$.BAs[*].AStrings[*]"
        )

    testCase "level 3 multiple string" (fun _ ->
            let x = RecordPath.Create(fun (c:C) -> c.CBs |> List.collect(fun b -> b.BAs |> List.collect (fun a -> a.AStrings))) 
            x.Path |> Expect.equal "should be correct path" "$.CBs[*].BAs[*].AStrings[*]"
        )
  ]
