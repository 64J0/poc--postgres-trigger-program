module Api.Types

open System

type IDatasource = Npgsql.NpgsqlDataSource

type ProgramsStorePath = string

type ProgramsDto =
    { Id: System.Guid option
      ProgramName: string
      ProgramFilePath: string option
      CreatedAt: DateTime }

type ProgramExecutionsDtoInput =
    { ProgramId: System.Guid
      ProgramInput: string
      CreatedAt: DateTime }

type ProgramExecutionsDtoOutput =
    { ProgramName: string
      ProgramFilePath: string option
      ProgramInput: string
      PullSuccess: bool option
      StdOutLog: string option
      StdErrLog: string option }

type ApplicationError =
    | Database of string
    | RequestFile of string
