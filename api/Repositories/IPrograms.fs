module Api.Repository.IPrograms

open Npgsql

open Api.Types

type IPrograms =
    abstract member DataSource: NpgsqlDataSource option with get, set

    abstract member create: dto: ProgramsDto -> Async<Result<unit, string>>

    abstract member read: unit -> Async<Result<ProgramsDto list, string>>

// abstract member update
// abstract member delete
