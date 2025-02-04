module Api.Database

open Npgsql
open Microsoft.Extensions.Logging

// Ideas:
// 1. Move this to a library project that is shared between API/ and Manager/;
// 2. Add a function that maps Npgsql extensions to F# Result types.
// 3. Remove dependency of Npgsql from repository interfaces

let getDatasource () : NpgsqlDataSource =
    match Api.Environment.DB_CONN with
    | Ok conn ->
        // https://www.npgsql.org/doc/diagnostics/logging.html?tabs=console
        let loggerFactory =
            LoggerFactory.Create(fun builder ->
                builder.AddConsole() |> ignore
                ())

        let dataSourceBuilder = new NpgsqlDataSourceBuilder(conn)
        dataSourceBuilder.UseLoggerFactory(loggerFactory) |> ignore
        dataSourceBuilder.Build()
    | Error err -> failwith err

/// Use this function whenever a value could be NULL from a SELECT query.
let tryGetValue<'T> (fn) (idx: int) : Option<'T> =
    try
        fn (idx) |> Some
    with _exn ->
        None
