namespace Manager

module Processor =

    open Fli
    open Npgsql
    open FsToolkit.ErrorHandling

    open Types

    /// If the `fn` returns a Result.Error, then log a message into the standard error stream,
    /// and returns unit.
    let private logError<'T, 'TError> (message: string) (fn: Async<Result<'T, 'TError>>) : Async<unit> =
        async {
            let! res = fn

            return
                match res with
                | Ok _ -> ()
                | Error err ->
                    eprintfn "An error happened when processing message %s: %A" message err
                    ()
        }

    let private runProgram (programFilePath: string) (programInput: string) : Async<Result<Output, ApplicationError>> =
        async {
            try
                let! output =
                    cli {
                        Exec "dotnet"
                        Arguments($"fsi {programFilePath} {programInput}")
                        CancelAfter 5_000 // TODO https://github.com/CaptnCodr/Fli/issues/79
                    }
                    |> Command.executeAsync

                return Ok output
            with exn ->
                return Error(ProgramExecutionError exn.Message)
        }

    /// 1. get the program details from database using the execution id
    /// 2. run the program and collect the result
    /// 3. call the next stage if the execution was a success or a failure
    let private handleExecuteProgram
        (executionId: int)
        (dataSource: NpgsqlDataSource)
        : Async<Result<ProgramOutputDto, ApplicationError>> =
        asyncResult {
            let! dbProgramExecution = Manager.Repositories.ProgramExecutions.getById dataSource executionId

            let! programFilePath =
                Option.map (fun x -> Ok x) dbProgramExecution.ProgramFilePath
                |> Option.defaultValue (
                    Error(
                        Types.ApplicationError.InvalidProgramExecution
                            $"No program file path was found for program execution {executionId}"
                    )
                )

            let! programExecutionRes = runProgram programFilePath dbProgramExecution.ProgramInput

            let programOutputDto: Types.ProgramOutputDto =
                { ExecutionId = executionId
                  ExecutionSuccess = (programExecutionRes.ExitCode = 0)
                  StatusCode = programExecutionRes.ExitCode
                  StdOutLog = programExecutionRes.Text
                  StdErrLog = programExecutionRes.Error
                  CreatedAt = System.DateTime.Now }

            return!
                match programOutputDto.ExecutionSuccess with
                | true -> Ok programOutputDto
                | false -> Error(ApplicationError.ProgramExecutionErrorDto programOutputDto)
        }

    /// 1. write the success output to the database
    /// No other side effects for now
    let private handleExecutionSuccess (programOutputDto: Types.ProgramOutputDto) (dataSource: NpgsqlDataSource) =
        Manager.Repositories.ProgramOutputs.create dataSource programOutputDto
        |> logError ($"handleExecutionSuccess for execution id {programOutputDto.ExecutionId}")

    /// 1. write the fail output to the database
    /// No retry for now
    let private handleExecutionFailure (programOutputDto: Types.ProgramOutputDto) (dataSource: NpgsqlDataSource) =
        Manager.Repositories.ProgramOutputs.create dataSource programOutputDto
        |> logError ($"handleExecutionFailure for execution id {programOutputDto.ExecutionId}")

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    try
                        printfn "Waiting new messages..."
                        let! msg = inbox.Receive()

                        match msg with
                        | Message.ExecuteProgram(executionId, dataSource) ->
                            let! programOutputDtoRes = handleExecuteProgram (executionId) (dataSource)

                            let message =
                                match programOutputDtoRes with
                                | Ok programOutputDto -> Message.HandleExecutionSuccess(programOutputDto, dataSource)
                                | Error(ApplicationError.ProgramExecutionErrorDto errOutputDto) ->
                                    Message.HandleExecutionFailure(errOutputDto, dataSource)
                                | Error(ApplicationError.Database msg)
                                | Error(ApplicationError.InvalidProgramExecution msg)
                                | Error(ApplicationError.ProgramExecutionError msg) ->
                                    Message.JustPrintErrorMessage(msg)

                            do inbox.Post(message)
                        | Message.HandleExecutionSuccess(programOutputDto, dataSource) ->
                            do! handleExecutionSuccess (programOutputDto) (dataSource)
                        | Message.HandleExecutionFailure(programOutputDto, dataSource) ->
                            do! handleExecutionFailure (programOutputDto) (dataSource)
                        | Message.JustPrintErrorMessage(message) ->
                            do eprintfn "An error happened when processing message %s" message

                        return! loop ()
                    with exn ->
                        eprintfn "Exception while handling new message: %A" exn
                        return! loop ()
                }

            loop ())
