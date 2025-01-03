module Api.Types

open System

type IDatasource = Npgsql.NpgsqlDataSource

type ProgramsDto =
    { Name: string
      DockerImage: string
      CreatedAt: DateTime }

type ProgramExecutionsDto =
    { Name: string
      DockerImage: string
      ProgramInput: string
      PullSuccess: bool
      StdOutLog: string
      StdErrLog: string }
