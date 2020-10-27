namespace Exair.Types

open Microsoft.FSharp.Quotations
open System

module private KeyHelpers =
    let jsonPath userExpr = 
        let rec innerLoop expr (lastName, acc) =
            match expr with
            |Patterns.Lambda(_, body) ->
                innerLoop body (lastName, acc)
            |Patterns.PropertyGet(Some parent, propInfo, []) ->
                let newState = 
                    match propInfo.PropertyType.GetInterface("System.Collections.IEnumerable") with
                    |_ when propInfo.PropertyType = typeof<string> -> propInfo.Name, sprintf ".%s%s" propInfo.Name acc
                    |null -> propInfo.Name, sprintf ".%s%s" propInfo.Name acc
                    |_ -> propInfo.Name, sprintf ".%s[*]%s" propInfo.Name acc
                newState |> innerLoop parent
            |Patterns.Call (None, _, expr1::[Patterns.Let (v, expr2, _)]) when v.Name = "mapping"->
                let _, parentPath = innerLoop expr1 ("","")
                let lastName, childPath = innerLoop expr2 ("","")
                lastName, parentPath + childPath
            |ExprShape.ShapeVar _ ->
                (lastName, acc)
            |_ -> 
                failwithf "Unsupported expression: %A" expr
        let lastName, path = innerLoop userExpr ("","")
        lastName, path|> sprintf "$%s"

type KeyCardinality =
    | SingleValue
    | MultiValue

type Key = 
    {
        Path:string
        Name:string
        KeyCardinality:KeyCardinality
        KeyDbType:string
        CollectionType:Type
        KeyType:Type
    }

    static member private CreateHelper (userExpr:Expr<('a -> 'b)>) keyCardinality keyType =
        match userExpr with
        |Patterns.WithValue(f, _, expr) ->
            let lastName, path = KeyHelpers.jsonPath expr
            {
                Path = path
                Name = lastName
                KeyCardinality = keyCardinality
                KeyDbType = keyType
                CollectionType = typeof<'a>
                KeyType = typeof<'b>
            }
        | _ -> failwithf "Unsupported expression: %A" userExpr

    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string)>) = Key.CreateHelper userExpr SingleValue "varchar(50)"
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int)>) = Key.CreateHelper userExpr SingleValue "int"
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> DateTime)>) = Key.CreateHelper userExpr SingleValue
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string list)>) = Key.CreateHelper userExpr MultiValue "varchar(50)"
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int list)>) = Key.CreateHelper userExpr MultiValue "int"