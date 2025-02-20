namespace Manager

module Main =

    open Npgsql
    open FsToolkit.ErrorHandling

    type Message =
        | ExecuteProgram of ExecutionId: int * DataSource: NpgsqlDataSource
        | HandleExecutionSuccess of unit
        | HandleExecutionFailure of unit

    let private handleExecuteProgram (executionId: int) (dataSource: NpgsqlDataSource) =
        // 1. get the program details from database using the execution id
        // 2. run the program and collect the result
        // 3. call the next stage if the execution was a success or a failure
        asyncResult {
            let! dbProgramExecution = Manager.Repositories.ProgramExecutions.getById dataSource executionId

            
        }
        
    let private handleExecutionSuccess () = ()
    let private handleExecutionFailure () = ()

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Message.ExecuteProgram(executionId, dataSource) ->
                        
                    | HandleExecutionSuccess ->
                        // 1. write the output to the database
                    | HandleExecutionFailure ->
                        // 1. write the output to the database
                        // 2. print to the error stream
                        // No retry for now
                })

    [<EntryPoint>]
    let main (_args: string[]) : int =
        let pgHandler dataSource =
            fun (_sender: obj) (eventArgs: NpgsqlNotificationEventArgs) ->
                printfn "Notification received"
                printfn "Event args payload: %A" eventArgs.Payload
                
                match System.Int32.TryParse eventArgs.Payload with
                | true, programExecutionId -> processor.Post(Message.ExecuteProgram(programExecutionId, dataSource))
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
