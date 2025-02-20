module Api.Repository.IPrograms

open Npgsql

open Api.Types

type IPrograms =
    abstract member DataSource: NpgsqlDataSource option with get, set

    abstract member create: dto: ProgramsDto -> Async<Result<unit, ApplicationError>>

    abstract member read: unit -> Async<Result<ProgramsDto list, ApplicationError>>

    abstract member update: programId: System.Guid -> programFilePath: string -> Async<Result<unit, ApplicationError>>
    
// abstract member delete
