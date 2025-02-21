module Api.Handlers.ProgramExecutions

open System.Net
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http

open Giraffe
open Api.Types

let createProgramExecution (programId: System.Guid) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetLogger()
        let serializer = ctx.GetJsonSerializer()
        let datasource = ctx.GetService<Api.Types.IDatasource>()

        let programExecutionsRepository =
            ctx.GetService<Api.Repository.IProgramExecutions.IProgramExecutions>()

        do programExecutionsRepository.DataSource <- Some datasource

        use _ = logger.BeginScope("CreateProgramExecution")

        task {
            try
                let body = ctx.Request.Body
                let! serializedBody = serializer.DeserializeAsync<ProgramExecutionsDtoInput> body
                logger.LogDebug "Body serialization complete"

                let! dbCreationResult =
                    programExecutionsRepository.create
                        { ProgramId = programId
                          ProgramInput = serializedBody.ProgramInput
                          CreatedAt = System.DateTime.Now }

                match dbCreationResult with
                | Ok() ->
                    logger.LogDebug "Database insertion complete"
                    ctx.SetStatusCode(int HttpStatusCode.Created)
                    return! json {| Message = "New execution inserted!" |} next ctx
                | Error err ->
                    logger.LogError $"Database insertion failed with error {err}"
                    ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                    return! json {| Message = "New execution was not inserted!" |} next ctx

            with exn ->
                logger.LogCritical $"Something wrong happened. Exception information: {exn}"
                ctx.SetStatusCode(int HttpStatusCode.InternalServerError)
                return! json {| Message = "Something wrong happened" |} next ctx
        }

let getProgramExecutions () : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetLogger()
        let datasource = ctx.GetService<Api.Types.IDatasource>()

        let programExecutionsRepository =
            ctx.GetService<Api.Repository.IProgramExecutions.IProgramExecutions>()

        do programExecutionsRepository.DataSource <- Some datasource

        use _ = logger.BeginScope("GetProgramExecutions")

        task {
            try
                let! dbPrograms = programExecutionsRepository.read ()

                match dbPrograms with
                | Ok p ->
                    logger.LogDebug "Database read complete"
                    ctx.SetStatusCode(int HttpStatusCode.OK)

                    return!
                        json
                            {| Message = "Read success!"
                               ProgramExecutions = p |}
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
