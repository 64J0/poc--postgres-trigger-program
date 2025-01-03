open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting

let endpoints =
    [ subRoute
          "/api"
          [ GET
                [ route "/programs" (text "TODO list programs")
                  route "/executions" (text "TODO list executions") ]

            POST
                [ route "/program" (Api.Handlers.Programs.createProgram ())
                  route "/execute" (text "TODO create new execution entry") ] ] ]

let notFoundHandler = "Not Found" |> text |> RequestErrors.notFound

let configureApp (appBuilder: IApplicationBuilder) =
    appBuilder.UseRouting().UseGiraffe(endpoints).UseGiraffe(notFoundHandler)

let configureServices (services: IServiceCollection) =
    services
        .AddSingleton<Api.Types.IDatasource>(Api.Database.getDatasource ())
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
