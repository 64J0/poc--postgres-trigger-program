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
                let! serializedBody = serializer.DeserializeAsync<ProgramsDto> body
                logger.LogDebug "Body serialization complete"

                let! dbCreationResult = programsRepository.create serializedBody

                match dbCreationResult with
                | Ok() ->
                    logger.LogDebug "Database insertion complete"
                    ctx.SetStatusCode(int HttpStatusCode.Created)
                    return! json {| Message = "New program inserted!" |} next ctx
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
