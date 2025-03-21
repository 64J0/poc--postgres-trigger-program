namespace Manager

module Processor =

    open Fli
    open Npgsql
    open FsToolkit.ErrorHandling

    open Types

    let private logError<'T, 'TError> (message: string) (fn: Async<Result<'T, 'TError>>) =
        async {
            let! res = fn

            match res with
            | Ok _ -> return ()
            | Error err -> eprintfn $"An error happened when processing message {message}: {err}"
        }

    let private runProgram (programFilePath: string) (programInput: string) : Async<Result<Output, ApplicationError>> =
        async {
            try
                let! output =
                    cli {
                        Exec "dotnet"
                        Arguments($"fsi {programFilePath} {programInput}")
                    }
                    |> Command.executeAsync

                return Ok output
            with exn ->
                return Error(ProgramExecutionError exn.Message)
        }

    let private handleExecuteProgram (executionId: int) (dataSource: NpgsqlDataSource) =
        // 1. get the program details from database using the execution id
        // 2. run the program and collect the result
        // 3. call the next stage if the execution was a success or a failure
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

            return programOutputDto
        }

    let private handleExecutionSuccess (programOutputDto: Types.ProgramOutputDto) (dataSource: NpgsqlDataSource) =
        asyncResult {
            let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto

            return ()
        }
        |> logError ($"handleExecutionSuccess for execution id {programOutputDto.ExecutionId}")

    let private handleExecutionFailure (programOutputDto: Types.ProgramOutputDto) (dataSource: NpgsqlDataSource) =
        asyncResult {
            let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto

            return ()
        }
        |> logError ($"handleExecutionFailure for execution id {programOutputDto.ExecutionId}")

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Message.ExecuteProgram(executionId, dataSource) ->
                        let! programOutputDtoRes = handleExecuteProgram (executionId) (dataSource)

                        match programOutputDtoRes with
                        | Ok programOutputDto ->

                            let message =
                                match programOutputDto.StatusCode with
                                | 0 -> Message.HandleExecutionSuccess(programOutputDto, dataSource)
                                | _ -> Message.HandleExecutionFailure(programOutputDto, dataSource)

                            inbox.Post(message) |> ignore
                        | Error err ->
                            logError ($"handleExecuteProgram error for execution id {executionId}: {err}.")
                            |> ignore

                        return! loop ()
                    | HandleExecutionSuccess(programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        do! handleExecutionSuccess (programOutputDto) (dataSource)
                        return! loop ()
                    | HandleExecutionFailure(programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        // 2. print to the error stream
                        // No retry for now
                        do! handleExecutionFailure (programOutputDto) (dataSource)
                        return! loop ()
                }

            loop ())
