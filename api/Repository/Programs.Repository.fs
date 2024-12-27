module Api.Repository.Programs

open Npgsql

let private testConnectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
let private dataSource = NpgsqlDataSource.Create(testConnectionString)

let getAll () =
    async {
        use command = dataSource.CreateCommand("SELECT * FROM programs")
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

        while! (reader.ReadAsync() |> Async.AwaitTask) do
            printfn "%s" (reader.GetString(0))
    }

let create () = ()
