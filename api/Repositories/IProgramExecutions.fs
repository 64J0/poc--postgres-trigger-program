module Api.Repository.IProgramExecutions

open Npgsql

open Api.Types

type IProgramExecutions =
    abstract member create:
        dataSource: NpgsqlDataSource -> dto: ProgramExecutionsDtoInput -> Async<Result<unit, string>>

    abstract member read: dataSource: NpgsqlDataSource -> Async<Result<ProgramExecutionsDtoOutput list, string>>

// abstract member update
// abstract member delete
