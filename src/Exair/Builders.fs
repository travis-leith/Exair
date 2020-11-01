[<AutoOpen>]
module Exair.Builders

type InsertBuilder<'a>(collection) =
    member __.Yield _ =
        {
            Collection = collection
            Values = []
        } : InsertQuery<'a>

    /// Sets the TABLE name for query
    //[<CustomOperation "table">]
    //member __.Table (state:InsertQuery<_>, name) = { state with Table = name }

    /// Sets the list of values for INSERT
    [<CustomOperation "values">]
    member __.Values (state:InsertQuery<'a>, values:'a list) = { state with Values = values }

    /// Sets the single value for INSERT
    [<CustomOperation "value">]
    member __.Value (state:InsertQuery<'a>, value:'a) = { state with Values = [value] }

type GetBuilder<'a>(collection) =
    member _.Yield _ =
        {
            Collection = collection
            EntityIds = []
        } : GetQuery<'a>

    /// Sets the list of ids for get
    [<CustomOperation "entityIds">]
    member __.EntityIds (state:GetQuery<'a>, entityIds:EntityId<'a> list) = { state with EntityIds = entityIds }

    /// Sets the single id for get
    [<CustomOperation "entityId">]
    member __.EntityId (state:GetQuery<'a>, entityId:EntityId<'a>) = { state with EntityIds = [entityId] }

type DeleteBuilder<'a>(collection) =
    member __.Yield _ =
        {
            Collection = collection
            Where = Where.Empty
        } : DeleteQuery<'a>

    /// Sets the TABLE name for query
    //[<CustomOperation "table">]
    //member __.Table (state:DeleteQuery, name) = { state with Table = name }

    /// Sets the WHERE condition
    [<CustomOperation "where">]
    member __.Where (state:DeleteQuery<'a>, where:Where) = { state with Where = where }

type UpdateBuilder<'a>(collection) =
    member __.Yield _ =
        {
            Collection = collection
            Value = Unchecked.defaultof<'a>
            Where = Where.Empty
        } : UpdateQuery<'a>

    /// Sets the TABLE name for query
    //[<CustomOperation "table">]
    //member __.Table (state:UpdateQuery<_>, name) = { state with Table = name }

    /// Sets the SET of value to UPDATE
    [<CustomOperation "set">]
    member __.Set (state:UpdateQuery<'a>, value:'a) = { state with Value = value }

    /// Sets the WHERE condition
    [<CustomOperation "where">]
    member __.Where (state:UpdateQuery<_>, where:Where) = { state with Where = where }

type SelectBuilder<'a>(collection) =
    member __.Yield _ =
        {
            Collection = collection
            Where = Where.Empty
            //OrderBy = []
            //Pagination = { Skip = 0; Take = None }
            //Joins = []
            //Aggregates = []
            //GroupBy = []
            //Distinct = false
        } : SelectQuery<'a>

    /// Sets the TABLE name for query
    //[<CustomOperation "table">]
    //member __.Table (state:SelectQuery<'a>, collection) = { state with Table = name }

    /// Sets the WHERE condition
    [<CustomOperation "where">]
    member __.Where (state:SelectQuery<'a>, where:Where) = { state with Where = where }

    ///// Sets the ORDER BY for multiple columns
    //[<CustomOperation "orderByMany">]
    //member __.OrderByMany (state:SelectQuery, values) = { state with OrderBy = values }

    ///// Sets the ORDER BY for single column
    //[<CustomOperation "orderBy">]
    //member __.OrderBy (state:SelectQuery, colName, direction) = { state with OrderBy = [(colName, direction)] }

    ///// Sets the SKIP value for query
    //[<CustomOperation "skip">]
    //member __.Skip (state:SelectQuery, skip) = { state with Pagination = { state.Pagination with Skip = skip } }
    
    ///// Sets the TAKE value for query
    //[<CustomOperation "take">]
    //member __.Take (state:SelectQuery, take) = { state with Pagination = { state.Pagination with Take = Some take } }

    ///// Sets the SKIP and TAKE value for query
    //[<CustomOperation "skipTake">]
    //member __.SkipTake (state:SelectQuery, skip, take) = { state with Pagination = { state.Pagination with Skip = skip; Take = Some take } }

    ///// INNER JOIN table where COLNAME equals to another COLUMN (including TABLE name)
    //[<CustomOperation "innerJoin">]
    //member __.InnerJoin (state:SelectQuery, tableName, colName, equalsTo) = { state with Joins = state.Joins @ [InnerJoin(tableName, colName, equalsTo)] }

    ///// LEFT JOIN table where COLNAME equals to another COLUMN (including TABLE name)
    //[<CustomOperation "leftJoin">]
    //member __.LeftJoin (state:SelectQuery, tableName, colName, equalsTo) = { state with Joins = state.Joins @ [LeftJoin(tableName, colName, equalsTo)] }
    
    ///// Sets the ORDER BY for multiple columns
    //[<CustomOperation "groupByMany">]
    //member __.GroupByMany (state:SelectQuery, values) = { state with GroupBy = values }

    ///// Sets the ORDER BY for single column
    //[<CustomOperation "groupBy">]
    //member __.GroupBy (state:SelectQuery, colName) = { state with GroupBy = [colName] }

    ///// COUNT aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation "count">]
    //member __.Count (state:SelectQuery, colName, alias) = { state with Aggregates = state.Aggregates @ [Aggregate.Count(colName, alias)] }

    ///// AVG aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation "avg">]
    //member __.Avg (state:SelectQuery, colName, alias) = { state with Aggregates = state.Aggregates @ [Aggregate.Avg(colName, alias)] }
    
    ///// SUM aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation "sum">]
    //member __.Sum (state:SelectQuery, colName, alias) = { state with Aggregates = state.Aggregates @ [Aggregate.Sum(colName, alias)] }
    
    ///// MIN aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation "min">]
    //member __.Min (state:SelectQuery, colName, alias) = { state with Aggregates = state.Aggregates @ [Aggregate.Min(colName, alias)] }
    
    ///// MIN aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation "max">]
    //member __.Max (state:SelectQuery, colName, alias) = { state with Aggregates = state.Aggregates @ [Aggregate.Max(colName, alias)] }
    
    /// Sets query to return DISTINCT values
    //[<CustomOperation "distinct">]
    //member __.Distinct (state:SelectQuery) = { state with Distinct = true }
    
let insertInto<'a> collection = InsertBuilder<'a>(collection)
let deleteFrom collection = DeleteBuilder<'a>(collection)
let updateInto<'a> collection = UpdateBuilder<'a>(collection)
let selectFrom<'a> collection = SelectBuilder<'a>(collection)
let getFrom<'a> collection = GetBuilder<'a>(collection)

/// Creates WHERE condition for column
let column key whereComp = Where.Key(key.KeyData, whereComp)
/// WHERE column value equals to
let eq (key:Key<'a,'b>) (o:'b) = column key (Eq o)
/// WHERE column value not equals to
let ne (key:Key<'a,'b>) (o:'b) = column key (Ne o)
/// WHERE column value greater than
let gt (key:Key<'a,'b>) (o:'b) = column key (Gt o)
/// WHERE column value lower than
let lt (key:Key<'a,'b>) (o:'b) = column key (Lt o)
/// WHERE column value greater/equals than
let ge (key:Key<'a,'b>) (o:'b) = column key (Ge o)
/// WHERE column value lower/equals than
let le (key:Key<'a,'b>) (o:'b) = column key (Le o)
///// WHERE column like value
//let like name (str:string) = column name (Like str)
/// WHERE column is IN values
let isIn (key:Key<'a,'b>) (os:'b list) =
    let objList = os |> List.map (fun x -> x :> obj)
    column key (In objList)
/// WHERE column is NOT IN values
let isNotIn (key:Key<'a,'b>) (os:'b list) =
    let objList = os |> List.map (fun x -> x :> obj)
    column key (NotIn objList)
///// WHERE column IS NULL
//let isNullValue name = column name IsNull
///// WHERE column IS NOT NULL
//let isNotNullValue name = column name IsNotNull