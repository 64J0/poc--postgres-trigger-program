namespace Shared.Database

open Npgsql
open Microsoft.Extensions.Logging
open FsToolkit.ErrorHandling

// Ideas:
// - Add a function that maps Npgsql extensions to F# Result types
// - Remove dependency of Npgsql from repository interfaces

module Main =

    /// Use this function to create an Npgsql datasource object with a console
    /// logger configured.
    let getDatasource () : Result<NpgsqlDataSource, Shared.Database.Environment.CustomError> =
        result {
            let! connStr = Shared.Database.Environment.DB_CONN

            // https://www.npgsql.org/doc/diagnostics/logging.html?tabs=console
            let loggerFactory =
                LoggerFactory.Create(fun builder ->
                    builder.AddConsole() |> ignore
                    ())

            let dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr)
            dataSourceBuilder.UseLoggerFactory(loggerFactory) |> ignore
            return dataSourceBuilder.Build()
        }

    /// Use this function whenever a value could be NULL from a SELECT query. It
    /// will turn this value into Option<'T> instead of throwing an exception.
    let tryGetValue<'T> (fn) (idx: int) : Option<'T> =
        try
            fn (idx) |> Some
        with _exn ->
            None

    /// Use this function to register a custom notification handler and start
    /// listening from a Postgres channel.
    ///
    /// - https://www.npgsql.org/doc/wait.html
    let getAsyncConnection
        (notificationEventHandler: NotificationEventHandler)
        : Result<NpgsqlConnection, Shared.Database.Environment.CustomError> =
        result {
            let! connStr = Shared.Database.Environment.DB_CONN
            let! pgChannel = Shared.Database.Environment.POSTGRES_CHANNEL

            let conn = new NpgsqlConnection(connStr)
            conn.Open()

            conn.Notification.AddHandler(notificationEventHandler)

            use cmd = new NpgsqlCommand($"LISTEN {pgChannel}", conn)
            cmd.ExecuteNonQuery() |> ignore
            return conn
        }
