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

    let private runProgram (programFilePath: string) (programInput: string) =
        cli {
            Exec "dotnet"
            Arguments($"fsi {programFilePath} {programInput}")
        }
        |> Command.executeAsync

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()
                    printfn "New message received..."

                    match msg with
                    | Message.ExecuteProgram(executionId, dataSource) ->
                        // 1. get the program details from database using the execution id
                        // 2. run the program and collect the result
                        // 3. call the next stage if the execution was a success or a failure
                        let! dbProgramExecution = Manager.Repositories.ProgramExecutions.getById dataSource executionId

                        match dbProgramExecution with
                        | Ok programExecution ->
                            let programFilePath =
                                Option.map (fun x -> Ok x) programExecution.ProgramFilePath
                                |> Option.defaultValue (
                                    Error(
                                        Types.ApplicationError.InvalidProgramExecution
                                            $"No program file path was found for program execution {executionId}"
                                    )
                                )

                            match programFilePath with
                            | Ok programFP ->
                                let! programExecutionRes = runProgram programFP programExecution.ProgramInput

                                let programOutputDto: Types.ProgramOutputDto =
                                    { ExecutionId = executionId
                                      ExecutionSuccess = (programExecutionRes.ExitCode = 0)
                                      StatusCode = programExecutionRes.ExitCode
                                      StdOutLog = programExecutionRes.Text
                                      StdErrLog = programExecutionRes.Error
                                      CreatedAt = System.DateTime.Now }

                                let message =
                                    match programExecutionRes.ExitCode with
                                    | 0 -> Message.HandleExecutionSuccess(programOutputDto, dataSource)
                                    | _ -> Message.HandleExecutionFailure(programOutputDto, dataSource)

                                inbox.Post(message)
                            | Error err ->
                                eprintfn
                                    $"Error when trying to get the program file path with execution id {executionId}: {err}"
                        | Error err ->
                            eprintfn
                                $"Error when trying to get the program execution entity with id {executionId} from the database: {err}"

                        return! loop ()
                    | HandleExecutionSuccess(programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto
                        return! loop ()
                    | HandleExecutionFailure(programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        // 2. print to the error stream
                        // No retry for now
                        let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto
                        return! loop ()
                }

            loop ())
