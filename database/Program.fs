namespace Shared.Database

open Npgsql
open Microsoft.Extensions.Logging

// Ideas:
// - Add a function that maps Npgsql extensions to F# Result types
// - Remove dependency of Npgsql from repository interfaces

module Main =

    let getDatasource () : NpgsqlDataSource =
        match Shared.Database.Environment.DB_CONN with
        | Ok connStr ->
            // https://www.npgsql.org/doc/diagnostics/logging.html?tabs=console
            let loggerFactory =
                LoggerFactory.Create(fun builder ->
                    builder.AddConsole() |> ignore
                    ())

            let dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr)
            dataSourceBuilder.UseLoggerFactory(loggerFactory) |> ignore
            dataSourceBuilder.Build()
        | Error err -> failwith err

    /// Use this function whenever a value could be NULL from a SELECT query.
    let tryGetValue<'T> (fn) (idx: int) : Option<'T> =
        try
            fn (idx) |> Some
        with _exn ->
            None

    type private MyNotificationEventHandlerDelegate() =
        static member handleNotification (_sender: obj) (_eventArgs: NpgsqlNotificationEventArgs) : unit =
            printfn "Received notification"

    // https://www.npgsql.org/doc/wait.html
    let getAsyncConnection (channelName: string) =
        match Shared.Database.Environment.DB_CONN with
        | Ok connStr ->
            let conn = new NpgsqlConnection(connStr)
            conn.Open()

            let notificationEventHandler =
                NotificationEventHandler(MyNotificationEventHandlerDelegate.handleNotification)

            conn.Notification.AddHandler(notificationEventHandler)

            use cmd = new NpgsqlCommand($"LISTEN {channelName}", conn)
            cmd.ExecuteNonQuery() |> ignore
            conn
        | Error err -> failwith err
