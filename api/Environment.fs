module Api.Environment

open System

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

let DB_CONN = tryGetEnvironmentVariable "DB_CONNECTION_STRING"
