open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting

let endpoints =
    [ subRoute
          "/api"
          [ GET
                [ route "/programs" (Api.Handlers.Programs.getPrograms ())
                  route "/executions" (Api.Handlers.ProgramExecutions.getProgramExecutions ()) ]

            POST
                [ route "/program" (Api.Handlers.Programs.createProgram ())
                  route "/execute" (Api.Handlers.ProgramExecutions.createProgramExecution ()) ] ] ]

let notFoundHandler = "Not Found" |> text |> RequestErrors.notFound

let configureApp (appBuilder: IApplicationBuilder) =
    appBuilder.UseRouting().UseGiraffe(endpoints).UseGiraffe(notFoundHandler)

let configureServices (services: IServiceCollection) =
    services
        .AddSingleton<Api.Types.IDatasource>(Api.Database.getDatasource ())
        .AddScoped<Api.Repository.IPrograms.IPrograms, Api.Repository.Programs.ProgramsRepository>()
        .AddScoped<
            Api.Repository.IProgramExecutions.IProgramExecutions,
            Api.Repository.ProgramExecutions.ProgramExecutionsRepository
          >()
        .AddRouting()
        .AddGiraffe()
    |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services

    let app = builder.Build()

    configureApp app
    app.Run()

    0
