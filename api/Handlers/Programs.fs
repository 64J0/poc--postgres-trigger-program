module Api.Handlers.Programs

open System.Net
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http

open Giraffe
open Api.Types

let createProgram () : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetLogger()
        let serializer = ctx.GetJsonSerializer()
        let datasource = ctx.GetService<Api.Types.IDatasource>()
        let programsRepository = ctx.GetService<Api.Repository.IPrograms.IPrograms>()
        do programsRepository.DataSource <- Some datasource

        use _ = logger.BeginScope("CreateProgram")

        task {
            try
                let body = ctx.Request.Body
                let! serializedBody = serializer.DeserializeAsync<ProgramsDtoInput> body
                logger.LogDebug "Body serialization complete"

                let newId = System.Guid.NewGuid()

                let! dbCreationResult =
                    programsRepository.create
                        { ProgramId = newId
                          ProgramName = serializedBody.ProgramName
                          CreatedAt = System.DateTime.Now }

                match dbCreationResult with
                | Ok() ->
                    logger.LogDebug "Database insertion complete"
                    ctx.SetStatusCode(int HttpStatusCode.Created)
                    return! json {| Message = $"New program inserted with id: {newId}!" |} next ctx
                | Error err ->
                    logger.LogError $"Database insertion failed with error {err}"
                    ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                    return! json {| Message = "New program was not inserted!" |} next ctx
            with exn ->
                logger.LogCritical $"Something wrong happened. Exception information: {exn}"
                ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                return! json {| Message = "Something wrong happened" |} next ctx
        }

let getPrograms () : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetLogger()
        let datasource = ctx.GetService<Api.Types.IDatasource>()
        let programsRepository = ctx.GetService<Api.Repository.IPrograms.IPrograms>()
        do programsRepository.DataSource <- Some datasource

        use _ = logger.BeginScope("GetPrograms")

        task {
            try
                let! dbPrograms = programsRepository.read ()

                match dbPrograms with
                | Ok p ->
                    logger.LogDebug "Database read complete"
                    ctx.SetStatusCode(int HttpStatusCode.OK)

                    return!
                        json
                            {| Message = "Read success!"
                               Programs = p |}
                            next
                            ctx
                | Error err ->
                    logger.LogError $"Database read failed with error {err}"
                    ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                    return! json {| Message = "Failed when trying to retrieve programs!" |} next ctx
            with exn ->
                logger.LogCritical $"Something wrong happened. Exception information: {exn}"
                ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                return! json {| Message = "Something wrong happened" |} next ctx
        }

let patchProgramFile (programId: System.Guid) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetLogger()
        let datasource = ctx.GetService<Api.Types.IDatasource>()
        let programsStorePath = ctx.GetService<Api.Types.ProgramsStorePath>()
        let programsRepository = ctx.GetService<Api.Repository.IPrograms.IPrograms>()
        do programsRepository.DataSource <- Some datasource

        use _ = logger.BeginScope("PatchProgramFile")

        let tryGetScriptFile (formFiles: IFormFileCollection) : Result<IFormFile, ApplicationError> =
            try
                formFiles.GetFile "script" |> Ok
            with _exn ->
                ApplicationError.RequestFile "No 'script' file present at the request" |> Error

        task {
            try
                let scriptFileResult = tryGetScriptFile ctx.Request.Form.Files
                logger.LogDebug $"Script file: {scriptFileResult}"

                match scriptFileResult with
                | Ok scriptFile ->
                    let scriptFilePath =
                        System.IO.Path.Combine([| programsStorePath; scriptFile.FileName |])
                        |> System.IO.Path.GetFullPath

                    use targetFilePath = System.IO.File.Create scriptFilePath
                    do! scriptFile.CopyToAsync targetFilePath |> Async.AwaitTask
                    targetFilePath.Flush()
                    targetFilePath.Close()

                    let! dbCreationResult = programsRepository.update programId scriptFilePath

                    match dbCreationResult with
                    | Ok() ->
                        logger.LogDebug "Database patch complete"
                        ctx.SetStatusCode(int HttpStatusCode.Accepted)
                        return! json {| Message = "Program file patched!" |} next ctx
                    | Error err ->
                        logger.LogError $"Database insertion failed with error {err}"
                        ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                        return! json {| Message = "Program file was not patched!" |} next ctx
                | Error err ->
                    logger.LogError $"{err}"
                    ctx.SetStatusCode(int HttpStatusCode.BadRequest)
                    return! json {| Message = $"ERROR: Program file not patched. {err}" |} next ctx
            with exn ->
                logger.LogCritical $"Something wrong happened. Exception information: {exn}"
                ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                return! json {| Message = "Something wrong happened" |} next ctx
        }
