module Api.Database

open Npgsql

let getDatasource () =
    match Api.Environment.DB_CONN with
    | Ok conn -> NpgsqlDataSource.Create(conn)
    | Error err -> failwith err
