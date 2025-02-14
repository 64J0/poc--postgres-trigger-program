namespace Manager

module Main =

    open Npgsql
    open FsToolkit.ErrorHandling

    type Message =
        | ExecuteProgram of ExecutionId: int
        | HandleExecutionSuccess of unit
        | HandleExecutionFailure of unit

    let processor =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Message.ExecuteProgram executionId ->
                        // 1. get the program details from database using the execution id
                        // 2. run the program and collect the result
                        // 3. call the next stage if the execution was a success or a failure
                    | HandleExecutionSuccess ->
                        // 1. write the output to the database
                    | HandleExecutionFailure ->
                        // 1. write the output to the database
                        // 2. print to the error stream
                })

    [<EntryPoint>]
    let main (_args: string[]) : int =
        result {
            let handler =
                fun (_sender: obj) (eventArgs: NpgsqlNotificationEventArgs) ->
                    printfn "Notification received"
                    printfn "Event args payload: %A" eventArgs.Payload

            let notificationEventHandler = NotificationEventHandler(handler)
            let! conn = Shared.Database.Main.getAsyncConnection notificationEventHandler

            while true do
                printfn "Started listening for Postgres notifications... "
                conn.Wait()

            return 0
        }
        |> Result.defaultWith (fun err ->
            eprintfn "Something wrong happened when starting the process. Error: %A" err
            1)
