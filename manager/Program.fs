namespace Manager

module Main =

    open Npgsql
    open FsToolkit.ErrorHandling
    open Fli

    type Message =
        | ExecuteProgram of processor: MailboxProcessor<Message> * ExecutionId: int * DataSource: NpgsqlDataSource
        | HandleExecutionSuccess of
            processor: MailboxProcessor<Message> *
            programOutputDto: Types.ProgramOutputDto *
            DataSource: NpgsqlDataSource
        | HandleExecutionFailure of
            processor: MailboxProcessor<Message> *
            programOutputDto: Types.ProgramOutputDto *
            DataSource: NpgsqlDataSource

    let private runProgram (programFilePath: string) (programInput: string) =
        cli {
            Exec "dotnet"
            Arguments($"fsi {programFilePath} {programInput}")
        }
        |> Command.executeAsync

    let private handleExecuteProgram
        (processor: MailboxProcessor<Message>)
        (executionId: int)
        (dataSource: NpgsqlDataSource)
        =
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

            let message =
                match programExecutionRes.ExitCode with
                | 0 -> Message.HandleExecutionSuccess(processor, programOutputDto, dataSource)
                | _ -> Message.HandleExecutionFailure(processor, programOutputDto, dataSource)

            return processor.Post(message)
        }

    let private handleExecutionSuccess
        (_processor: MailboxProcessor<Message>)
        (programOutputDto: Types.ProgramOutputDto)
        (dataSource: NpgsqlDataSource)
        =
        asyncResult {
            let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto

            return ()
        }

    let private handleExecutionFailure
        (_processor: MailboxProcessor<Message>)
        (programOutputDto: Types.ProgramOutputDto)
        (dataSource: NpgsqlDataSource)
        =
        asyncResult {
            let! _ = Manager.Repositories.ProgramOutputs.create dataSource programOutputDto

            return ()
        }

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Message.ExecuteProgram(processor, executionId, dataSource) ->
                        let! _ = handleExecuteProgram (processor) (executionId) (dataSource)
                        return! loop ()
                    | HandleExecutionSuccess(processor, programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        let! _ = handleExecutionSuccess (processor) (programOutputDto) (dataSource)
                        return! loop ()
                    | HandleExecutionFailure(processor, programOutputDto, dataSource) ->
                        // 1. write the output to the database
                        // 2. print to the error stream
                        // No retry for now
                        let! _ = handleExecutionFailure (processor) (programOutputDto) (dataSource)
                        return! loop ()
                }

            loop ())

    [<EntryPoint>]
    let main (_args: string[]) : int =
        let pgHandler dataSource =
            fun (_sender: obj) (eventArgs: NpgsqlNotificationEventArgs) ->
                printfn "Notification received"
                printfn "Event args payload: %A" eventArgs.Payload

                match System.Int32.TryParse eventArgs.Payload with
                | true, programExecutionId ->
                    let message = Message.ExecuteProgram(processor, programExecutionId, dataSource)
                    processor.Post(message)
                | false, _ -> eprintfn "It was not possible to parse %A to integer" eventArgs.Payload

        result {
            let! dataSource = Shared.Database.Main.getDatasource ()

            let notificationEventHandler = NotificationEventHandler(pgHandler dataSource)
            let! conn = Shared.Database.Main.getAsyncConnection notificationEventHandler

            while true do
                printfn "Started listening for Postgres notifications... "
                conn.Wait()

            return 0
        }
        |> Result.defaultWith (fun err ->
            eprintfn "Something wrong happened when starting the process. Error: %A" err
            1)
