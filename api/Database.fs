module Api.Database

open Npgsql

// Ideas:
// 1. Move this to a library project that is shared between API/ and Manager/;
// 2. Add a function that maps Npgsql extensions to F# Result types.
// 3. Remove dependency of Npgsql from repository interfaces

let getDatasource () : NpgsqlDataSource =
    match Api.Environment.DB_CONN with
    | Ok conn ->
        let dataSourceBuilder = new NpgsqlDataSourceBuilder(conn)
        let loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory()
        do dataSourceBuilder.UseLoggerFactory(loggerFactory) |> ignore
        dataSourceBuilder.Build()
    | Error err -> failwith err

/// Use this function whenever a value could be NULL from a SELECT query.
let tryGetValue<'T> (fn) (idx: int) : Option<'T> =
    try
        fn (idx) |> Some
    with _exn ->
        None
