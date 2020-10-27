namespace Exair.Types
open Thoth.Json.Net
open System

module TypeCoder =
    let encoder (t:Type) = Encode.string t.FullName
    let decoder path (typeName:JsonValue) =
        try
            Decode.string path typeName
            |> Result.map (fun s -> Type.GetType(s))
        with
        |ex -> Decode.fail ex.Message path typeName