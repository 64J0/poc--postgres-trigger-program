namespace Manager

module Main =

    open Npgsql
    open FsToolkit.ErrorHandling

    open Manager.Types
    open Manager.Processor

    [<EntryPoint>]
    let main (_args: string[]) : int =
        let pgHandler dataSource =
            fun (_sender: obj) (eventArgs: NpgsqlNotificationEventArgs) ->
                printfn "Notification received"
                printfn "Event args payload: %A" eventArgs.Payload

                match System.Int32.TryParse eventArgs.Payload with
                | true, programExecutionId ->
                    let message = Message.ExecuteProgram(programExecutionId, dataSource)
                    processor.Post(message)
                | false, _ -> eprintfn "It was not possible to parse %A to integer" eventArgs.Payload

        result {
            let! dataSource = Shared.Database.Main.getDatasource ()

            let notificationEventHandler = NotificationEventHandler(pgHandler dataSource)
            let! conn = Shared.Database.Main.getAsyncConnection notificationEventHandler

            while true do
                printfn "Waiting for next Postgres notification... "
                conn.Wait()

            return 0
        }
        |> Result.defaultWith (fun err ->
            eprintfn "Something wrong happened when starting the process. Error: %A" err
            1)
