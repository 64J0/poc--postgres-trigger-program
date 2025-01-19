module Api.Database

open Npgsql

// Ideas:
// 1. Move this to a library project that is shared between API/ and Manager/;
// 2. Add a function that maps Npgsql extensions to F# Result types.

let getDatasource () : NpgsqlDataSource =
    match Api.Environment.DB_CONN with
    | Ok conn -> NpgsqlDataSource.Create(conn)
    | Error err -> failwith err

/// Use this function whenever a value could be NULL from a SELECT query.
let tryGetValue<'T> (fn) (idx: int) : Option<'T> =
    try
        fn (idx) |> Some
    with _exn ->
        None
