module Api.Repository.IPrograms

open Npgsql

open Api.Types

type IPrograms =
    abstract member DataSource: NpgsqlDataSource option with get, set

    abstract member create: dto: ProgramsDtoToDB -> Async<Result<unit, ApplicationError>>

    abstract member read: unit -> Async<Result<ProgramsDtoFromDB list, ApplicationError>>

    abstract member update: programId: System.Guid -> programFilePath: string -> Async<Result<unit, ApplicationError>>

// abstract member delete
