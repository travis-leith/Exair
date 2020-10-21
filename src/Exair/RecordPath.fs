[<AutoOpen>]
module RecordPath
open Microsoft.FSharp.Quotations
open System
let private jsonPath userExpr = 
    let rec innerLoop expr state =
        match expr with
        |Patterns.Lambda(_, body) ->
            innerLoop body state
        |Patterns.PropertyGet(Some parent, propInfo, []) ->
            let newState = 
                match propInfo.PropertyType.GetInterface(nameof Collections.IEnumerable) with
                |_ when propInfo.PropertyType = typeof<string> -> sprintf ".%s%s" propInfo.Name state
                |null -> sprintf ".%s%s" propInfo.Name state
                |_ -> sprintf ".%s[*]%s" propInfo.Name state
            newState |> innerLoop parent
        |Patterns.Call (None, _, expr1::[Patterns.Let (v, expr2, _)]) when v.Name = "mapping"->
            let parentPath = innerLoop expr1 ""
            let childPath = innerLoop expr2 ""
            parentPath + childPath
        |ExprShape.ShapeVar _ ->
            state
        |_ -> 
            failwithf "Unsupported expression: %A" expr
    innerLoop userExpr "" |> sprintf "$%s"

type RecordPath<'a, 'b> = 
    private {
        GetPrivate: 'a -> 'b
        PathPrivate:string
    }
    member this.Path = this.PathPrivate

type RecordPath =
    static member private CreatePrivate (userExpr:Expr<('a -> 'b)>) =
        match userExpr with
        |Patterns.WithValue(f, _, expr) ->
            let path = jsonPath expr
            {
                GetPrivate = f :?> ('a -> 'b)
                PathPrivate = path
            }
        | _ -> failwithf "Unsupported expression: %A" userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string)>) = RecordPath.CreatePrivate userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int)>) = RecordPath.CreatePrivate userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> DateTime)>) = RecordPath.CreatePrivate userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string list)>) = RecordPath.CreatePrivate userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int list)>) = RecordPath.CreatePrivate userExpr