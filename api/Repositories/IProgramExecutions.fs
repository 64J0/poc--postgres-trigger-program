module Api.Repository.IProgramExecutions

open Npgsql

open Api.Types

type IProgramExecutions =
    abstract member DataSource: NpgsqlDataSource option with get, set

    abstract member create: dto: ProgramExecutionsDtoInput -> Async<Result<unit, string>>

    abstract member read: unit -> Async<Result<ProgramExecutionsDtoOutput list, string>>

// abstract member update
// abstract member delete
