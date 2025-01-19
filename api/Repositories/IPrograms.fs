module Api.Repository.IPrograms

open Npgsql

open Api.Types

type IPrograms =
    abstract member create: dataSource: NpgsqlDataSource -> dto: ProgramsDto -> Async<Result<unit, string>>

    abstract member read: dataSource: NpgsqlDataSource -> Async<Result<ProgramsDto list, string>>

// abstract member update
// abstract member delete
