namespace Exair
open System
open Microsoft.FSharp.Quotations


type Database = Database of string with member this.AsString = match this with Database s -> s

type EntityId<'a> = EntityId of uint64 with member this.AsInt = match this with EntityId i -> i

type Entity<'a> = {
    EntityId:EntityId<'a>
    LastUpdate:DateTimeOffset
    VersionNumber:int
    Item:'a
}

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

    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string)>) = Key.CreateHelper userExpr SingleValue "varchar(200)"
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int)>) = Key.CreateHelper userExpr SingleValue "int"
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> DateTime)>) = Key.CreateHelper userExpr SingleValue
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string list)>) = Key.CreateHelper userExpr MultiValue "varchar(50)"
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int list)>) = Key.CreateHelper userExpr MultiValue "int"

type Collection<'a> = internal {
    Database: Database
    CollectionType: Type
    TableName: string
    SearchKeys: Key list
    UniqueKeys: Key list
    //ForeignKeys: (Key * string) list
}

module Collection =
    let OfType<'a> database : Collection<'a> = {
        CollectionType = typeof<'a>
        Database = database
        TableName = sprintf "Main_%s" typeof<'a>.Name
        SearchKeys = []
        UniqueKeys = []
        //ForeignKeys = []
    }

    let WithSearchKey key def =
        {def with SearchKeys = key::def.SearchKeys}

    let WithUniqueKey key def =
        {def with UniqueKeys = key::def.UniqueKeys}

    //let WithForeignKey<'a> key def =
    //    let typeName = typeof<'a>.Name
    //    {def with ForeignKeys = (key, typeName)::def.ForeignKeys}

//type OrderDirection =
//    | Asc
//    | Desc

//type OrderBy = Key * OrderDirection

type ColumnComparison =
    | Eq of obj
    | Ne of obj
    | Gt of obj
    | Lt of obj
    | Ge of obj
    | Le of obj
    | In of obj list
    | NotIn of obj list
    //| Like of string
    //| IsNull
    //| IsNotNull

type BinaryOperation =
    | And
    | Or

type UnaryOperation =
    | Not

type Where =
    | Empty
    | Key of Key * ColumnComparison
    | Binary of Where * BinaryOperation * Where
    | Unary of UnaryOperation * Where
    static member (|||) (a, b) = Binary(a, Or, b)
    static member (&&&) (a, b) = Binary(a, And, b)
    static member (!!!) a = Unary (Not, a)

type Pagination = {
    Skip : int
    Take : int option
}
    
//type Join =
//    | InnerJoin of table:string * colName:string * equalsToColumn:string
//    | LeftJoin of table:string * colName:string * equalsToColumn:string

//module Join =
//    let tableName = function
//        | InnerJoin (t,_,_)
//        | LeftJoin (t,_,_) -> t

//type Aggregate =
//    | Count of columnName:string * alias:string
//    | Avg of columnName:string * alias:string
//    | Sum of columnName:string * alias:string
//    | Min of columnName:string * alias:string
//    | Max of columnName:string * alias:string

type SelectQuery<'a> = {
    Collection : Collection<'a>
    Where : Where
    //OrderBy : OrderBy list
    //Pagination : Pagination
    //Joins : Join list
    //Aggregates : Aggregate list
    //GroupBy : string list
    //Distinct : bool
}

type InsertQuery<'a> = {
    Collection : Collection<'a>
    Values : 'a list
}

type GetQuery<'a> = {
    Collection : Collection<'a>
    EntityIds : EntityId<'a> list
}

type UpdateQuery<'a> = {
    Collection : Collection<'a>
    Value : 'a
    Where : Where
}

type DeleteQuery<'a> = {
    Collection : Collection<'a>
    Where : Where
}
