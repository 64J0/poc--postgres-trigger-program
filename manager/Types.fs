module Manager.Types

open Npgsql

type ProgramExecutionsDto =
    { Id: int
      ProgramName: string
      ProgramInput: string
      CreatedAt: System.DateTime
      ProgramFilePath: string option }

type ProgramOutputDto =
    { ExecutionId: int
      ExecutionSuccess: bool
      StatusCode: int
      StdOutLog: string option
      StdErrLog: string option
      CreatedAt: System.DateTime }

type ApplicationError =
    | Database of string
    | InvalidProgramExecution of string
    | ProgramExecutionError of string
    | ProgramExecutionErrorDto of ProgramOutputDto

type Message =
    | ExecuteProgram of ExecutionId: int * DataSource: NpgsqlDataSource
    | HandleExecutionSuccess of programOutputDto: ProgramOutputDto * DataSource: NpgsqlDataSource
    | HandleExecutionFailure of programOutputDto: ProgramOutputDto * DataSource: NpgsqlDataSource
    | JustPrintErrorMessage of message: string
