module Api.Database

open Npgsql

let private testConnectionString =
    "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase"

let getDatasource () =
    NpgsqlDataSource.Create(testConnectionString)
