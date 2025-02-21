module Manager.Types

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
