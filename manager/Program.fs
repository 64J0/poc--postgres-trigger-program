namespace Manager

module Main =

    open Npgsql
    open FsToolkit.ErrorHandling

    type Message = ExecuteProgram of ExecutionId: int

    type Manager = MailboxProcessor<Message>

    let processor (manager: Manager) =
        let rec loop () =
            async {
                let! message = manager.Receive()

                match message with
                | ExecuteProgram _executionId -> return! loop ()
            }

        loop

    let start () =
        MailboxProcessor.Start(fun manager -> processor manager ())

    let executeProgram (manager: Manager) =
        manager.Post(ExecuteProgram 1)
        manager

    [<EntryPoint>]
    let main (_args: string[]) : int =
        result {
            let handler =
                fun (_sender: obj) (eventArgs: NpgsqlNotificationEventArgs) ->
                    printfn "Received notification"
                    printfn "Event args payload: %A" eventArgs.Payload

            let notificationEventHandler = NotificationEventHandler(handler)
            let! conn = Shared.Database.Main.getAsyncConnection notificationEventHandler

            while true do
                conn.Wait()

            return 0
        }
        |> Result.defaultWith (fun err ->
            eprintfn "Something wrong happened when starting the process. Error: %A" err
            1)
