namespace Exair
open System
open Microsoft.FSharp.Quotations


type Database = Database of string with member this.AsString = let (Database x) = this in x

type EntityId<'a> = EntityId of uint64 with member this.AsInt = let (EntityId x) = this in x

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

type JsonPath = JsonPath of string with member this.AsString = let (JsonPath s) = this in s

type KeyData =
    internal {
        JsonPath:JsonPath
        Name:string
        KeyCardinality:KeyCardinality
        KeyDbType:string
    }

type Key<'collection, 'a> =
    internal {
        Get:'collection -> 'a
        KeyData:KeyData
    }
    
type Key =
    static member private CreateHelper (userExpr:Expr<('collection -> 'a)>) keyCardinality keyType =
        match userExpr with
        |Patterns.WithValue(f, _, expr) ->
            let lastName, path = KeyHelpers.jsonPath expr
            let keyData = {
                Name = lastName
                JsonPath = JsonPath path
                KeyCardinality = keyCardinality
                KeyDbType = keyType
            }
            {
                Get = f :?> 'collection -> 'a
                KeyData = keyData
            }
        | _ -> failwithf "Unsupported expression: %A" userExpr
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('collection -> string)>) = Key.CreateHelper userExpr SingleValue "varchar(200)"
    static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('collection -> int)>) = Key.CreateHelper userExpr SingleValue "int"
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> DateTime)>) = Key.CreateHelper userExpr SingleValue
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> string list)>) = Key.CreateHelper userExpr MultiValue "varchar(50)"
    //static member Create ([<ReflectedDefinition(true)>] userExpr:Expr<('a -> int list)>) = Key.CreateHelper userExpr MultiValue "int"

type Collection<'a> = internal {
    Database: Database
    TableName: string
    SearchKeys: KeyData list
    UniqueKeys: KeyData list
    //ForeignKeys: (Key * string) list
}

module Collection =
    let OfType<'a> database : Collection<'a> = {
        Database = database
        TableName = sprintf "Main_%s" typeof<'a>.Name
        SearchKeys = []
        UniqueKeys = []
        //ForeignKeys = []
    }

    let WithSearchKey (key:Key<'a,'b>) (collection:Collection<'a>) =
        {collection with SearchKeys = key.KeyData::collection.SearchKeys}

    let WithUniqueKey (key:Key<'a,'b>) (collection:Collection<'a>) =
        {collection with UniqueKeys = key.KeyData::collection.UniqueKeys}

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
    | Key of KeyData * ColumnComparison
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

type SelectQuery<'a> = internal {
    Collection : Collection<'a>
    Where : Where
    //OrderBy : OrderBy list
    //Pagination : Pagination
    //Joins : Join list
    //Aggregates : Aggregate list
    //GroupBy : string list
    //Distinct : bool
}

type InsertQuery<'a> = internal {
    Collection : Collection<'a>
    Values : 'a list
}

type GetQuery<'a> = internal {
    Collection : Collection<'a>
    EntityIds : EntityId<'a> list
}

type UpdateQuery<'a> = internal {
    Collection : Collection<'a>
    Value : 'a
    Where : Where
}

type DeleteQuery<'a> = internal {
    Collection : Collection<'a>
    Where : Where
}
