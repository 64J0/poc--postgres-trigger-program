namespace Shared.Database

open Npgsql
open Microsoft.Extensions.Logging

// Ideas:
// - Add a function that maps Npgsql extensions to F# Result types
// - Remove dependency of Npgsql from repository interfaces

module Main =
    
    let getDatasource () : NpgsqlDataSource =
        match Shared.Database.Environment.DB_CONN with
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

