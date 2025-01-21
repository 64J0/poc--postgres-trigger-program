module Api.Repository.Programs

open Npgsql

open Api.Repository.IPrograms
open Api.Types

type ProgramsRepository() =
    member private this.dbCreate (dataSource: NpgsqlDataSource) (dto: ProgramsDto) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                        INSERT INTO programs
                        (name, docker_image, created_at)
                        VALUES ($1, $2, $3);
                        """
                )

            command.Parameters.AddWithValue(dto.Name) |> ignore
            command.Parameters.AddWithValue(dto.DockerImage) |> ignore
            command.Parameters.AddWithValue(dto.CreatedAt) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }

    member private this.dbRead(dataSource: NpgsqlDataSource) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                        SELECT
                          p.name,
                          p.docker_image,
                          p.created_at
                        FROM programs p;
                        """
                )

            use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

            let mutable dbResponse = []

            while! (reader.ReadAsync() |> Async.AwaitTask) do
                let name = reader.GetString(0)
                let dockerImage = reader.GetString(1)
                let createdAt = reader.GetDateTime(2)

                dbResponse <-
                    { Name = name
                      DockerImage = dockerImage
                      CreatedAt = createdAt }
                    :: dbResponse

            return Ok dbResponse
        }

    interface IPrograms with
        member val DataSource = None with get, set

        member this.create(dto: ProgramsDto) =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbCreate (dataSource) (dto)
            | None -> Error "DataSource object was not set" |> async.Return

        member this.read() =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbRead (dataSource)
            | None -> Error "DataSource object was not set" |> async.Return
