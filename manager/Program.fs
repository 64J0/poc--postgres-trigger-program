namespace Manager

module Main =

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
        let conn = Shared.Database.Main.getAsyncConnection "program_manager_channel"

        while true do
            conn.Wait()

        0
