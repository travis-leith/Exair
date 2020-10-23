namespace Exair

open Microsoft.FSharp.Quotations
open System

module private KeyHelpers =
    let jsonPath userExpr = 
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

type KeyCardinality =
    | SingleValue
    | MultiValue

module KeyConstraint =
    type KeyConstraintValue =
        private 
        | UniqueKeyPrivate
        | ForeignKeyPrivate of string

    let CreateUnique = UniqueKeyPrivate
    let CreateForeign<'a> = typeof<'a>.Name |> ForeignKeyPrivate

    let (|UniqueKey|ForeignKey|) = function
        | UniqueKeyPrivate -> UniqueKey
        | ForeignKeyPrivate s -> ForeignKey s

type Key = 
    {
        Path:String
        KeyCardinality:KeyCardinality
        KeyConstraint:KeyConstraint.KeyConstraintValue option
    }

    static member private CreateHelper (userExpr:Expr<('a -> 'b)>) keyCardinality  =
        match userExpr with
        |Patterns.WithValue(f, _, expr) ->
            let path = KeyHelpers.jsonPath expr
            {
                Path = path
                KeyCardinality = keyCardinality
                KeyConstraint = None
            }
        | _ -> failwithf "Unsupported expression: %A" userExpr

    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string)>) = Key.CreateHelper userExpr SingleValue
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int)>) = Key.CreateHelper userExpr SingleValue
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> DateTime)>) = Key.CreateHelper userExpr SingleValue
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string list)>) = Key.CreateHelper userExpr MultiValue
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int list)>) = Key.CreateHelper userExpr MultiValue
    member this.WithConstraint keyConstraint = {this with KeyConstraint = Some keyConstraint}