module Api.Repository.ProgramExecutions

open Npgsql

let private testConnectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
let private dataSource = NpgsqlDataSource.Create(testConnectionString)

let getAll () =
    async {
        use command = dataSource.CreateCommand("""
        SELECT * FROM program_executions pe
        JOIN program_outputs po
        ON po.execution_id = pe.id
        """)
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

        while! (reader.ReadAsync() |> Async.AwaitTask) do
            printfn "%s" (reader.GetString(0))
    }

let create () = ()
