namespace Exair.Types
open System
open MySqlConnector

type Collection<'a> = internal {
    Database: Database
    CollectionType: Type
    TableName: string
    SearchKeys: Key list
    UniqueKeys: Key list
    ForeignKeys: (Key * string) list
    Connection: MySqlConnection option
}

