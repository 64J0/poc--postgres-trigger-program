namespace Shared.Database

open System

module Environment =

    let private tryGetEnvironmentVariable (envVar: string) : Result<string, string> =
        try
            Environment.GetEnvironmentVariable envVar |> Ok
        with
        | :? ArgumentNullException as exn ->
            eprintfn $"Error because environment variable {envVar} is not set! Exception: {exn}."
            Error $"Error because environment variable {envVar} is not set!"
        | :? Security.SecurityException as exn ->
            eprintfn $"Error due to lack of necessary permissions for environment variable {envVar}! Exception: {exn}"
            Error $"Error due to lack of necessary permissions for environment variable {envVar}!"

#if DEBUG
    let DB_CONN =
        Ok
            "Host=localhost;Username=postgres;Password=changeme;Database=postgres;Connection Pruning Interval=1;Connection Idle Lifetime=2;Enlist=false;No Reset On Close=true"
#else
    let DB_CONN = tryGetEnvironmentVariable "DB_CONNECTION_STRING"
#endif
