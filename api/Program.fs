open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting
open FsToolkit.ErrorHandling

type ReturnCode() =
    static member Ok = 0
    static member Error = 1

let endpoints =
    [ subRoute
          "/api"
          [ GET
                [ route "/programs" (Api.Handlers.Programs.getPrograms ())
                  route "/executions" (Api.Handlers.ProgramExecutions.getProgramExecutions ()) ]

            POST
                [ route "/program" (Api.Handlers.Programs.createProgram ())
                  route "/execution" (Api.Handlers.ProgramExecutions.createProgramExecution ()) ]

            PATCH [ routef "/program/%O" Api.Handlers.Programs.patchProgramFile ] ] ]

let notFoundHandler = "Not Found" |> text |> RequestErrors.notFound

let configureApp (appBuilder: IApplicationBuilder) =
    appBuilder.UseRouting().UseGiraffe(endpoints).UseGiraffe(notFoundHandler)

let configureServices (services: IServiceCollection) =
    result {
        let! dataSource = Shared.Database.Main.getDatasource ()
        let! programsStore = Shared.Database.Environment.PROGRAMS_STORE

        services
            .AddSingleton<Api.Types.IDatasource>(dataSource)
            .AddSingleton<Api.Types.ProgramsStorePath>(programsStore)
            .AddScoped<Api.Repository.IPrograms.IPrograms, Api.Repository.Programs.ProgramsRepository>()
            .AddScoped<
                Api.Repository.IProgramExecutions.IProgramExecutions,
                Api.Repository.ProgramExecutions.ProgramExecutionsRepository
              >()
            .AddRouting()
            .AddGiraffe()
        |> ignore

        return ()
    }

[<EntryPoint>]
let main args : int =
    result {
        let builder = WebApplication.CreateBuilder(args)
        do! configureServices builder.Services

        let app = builder.Build()

        configureApp app
        app.Run()

        return ReturnCode.Ok
    }
    |> Result.defaultWith (fun err ->
        eprintfn "Something wrong happened when starting the process. Error: %A" err
        ReturnCode.Error)
