module Api.Types

type IDatasource = Npgsql.NpgsqlDataSource

type ProgramsStorePath = string

type ProgramsDtoInput = { ProgramName: string }

type ProgramsDtoToDB =
    { ProgramId: System.Guid
      ProgramName: string
      CreatedAt: System.DateTime }

type ProgramsDtoFromDB =
    { ProgramId: System.Guid
      ProgramName: string
      CreatedAt: System.DateTime }

type ProgramExecutionsDtoInput = { ProgramInput: string }

type ProgramExecutionsDtoToDB =
    { ProgramId: System.Guid
      ProgramInput: string
      CreatedAt: System.DateTime }

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
