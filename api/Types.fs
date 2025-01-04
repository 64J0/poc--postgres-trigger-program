module Api.Types

open System

type IDatasource = Npgsql.NpgsqlDataSource

type ProgramsDto =
    { Name: string
      DockerImage: string
      CreatedAt: DateTime }

type ProgramExecutionsDtoInput =
    { Name: string
      ProgramInput: string
      CreatedAt: DateTime }

type ProgramExecutionsDtoOutput =
    { Name: string
      DockerImage: string
      ProgramInput: string
      PullSuccess: bool
      StdOutLog: string
      StdErrLog: string }
