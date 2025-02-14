namespace Shared.Database

open System

module Environment =

    type CustomError =
        | ArgumentNull of string
        | Security of string

    let private tryGetEnvironmentVariable (envVar: string) : Result<string, CustomError> =
        try
            Environment.GetEnvironmentVariable envVar |> Ok
        with
        | :? ArgumentNullException as exn ->
            eprintfn $"Error because environment variable {envVar} is not set! Exception: {exn}."

            CustomError.ArgumentNull $"Error because environment variable {envVar} is not set!"
            |> Error
        | :? Security.SecurityException as exn ->
            eprintfn $"Error due to lack of necessary permissions for environment variable {envVar}! Exception: {exn}"

            CustomError.Security $"Error due to lack of necessary permissions for environment variable {envVar}!"
            |> Error

#if DEBUG
    let DB_CONN =
        Ok
            "Host=localhost;Username=postgres;Password=changeme;Database=postgres;Connection Pruning Interval=1;Connection Idle Lifetime=2;Enlist=false;No Reset On Close=true"

    let POSTGRES_CHANNEL = Ok "program_manager_channel"
#else
    let DB_CONN = tryGetEnvironmentVariable "DB_CONNECTION_STRING"
    let POSTGRES_CHANNEL = tryGetEnvironmentVariable "POSTGRES_CHANNEL"
#endif
