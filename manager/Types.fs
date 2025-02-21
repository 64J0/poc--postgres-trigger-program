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

type Message =
    | ExecuteProgram of processor: MailboxProcessor<Message> * ExecutionId: int * DataSource: NpgsqlDataSource
    | HandleExecutionSuccess of
        processor: MailboxProcessor<Message> *
        programOutputDto: ProgramOutputDto *
        DataSource: NpgsqlDataSource
    | HandleExecutionFailure of
        processor: MailboxProcessor<Message> *
        programOutputDto: ProgramOutputDto *
        DataSource: NpgsqlDataSource
