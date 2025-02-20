module Manager.Types

type ProgramExecutionsDto =
    { Id: int
      ProgramName: string
      ProgramInput: string
      CreatedAt: System.DateTime }

type ProgramOutputDto =
    { ExecutionId: int
      ExecutionSuccess: bool
      StatusCode: int
      StdOutLog: string
      StdErrLog: string
      CreatedAt: System.DateTime }

type ApplicationError = Database of string
