module Api.Repository.Programs

open Npgsql

open Api.Repository.IPrograms
open Api.Types

type ProgramsRepository() =
    member private _.dbCreate (dataSource: NpgsqlDataSource) (dto: ProgramsDto) =
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

    member private _.dbRead(dataSource: NpgsqlDataSource) =
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

    member private _.dbUpdate (dataSource: NpgsqlDataSource) (dto: ProgramsDto) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    UPDATE programs
                    SET name = $1
                    WHERE 
                    docker_image = $2;
                    """
                )

            command.Parameters.AddWithValue(dto.Name) |> ignore
            command.Parameters.AddWithValue(dto.DockerImage) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }

    member private _.dbDelete (dataSource: NpgsqlDataSource) (dto: ProgramsDto) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    DELETE FROM programs
                    WHERE docker_image = $1;
                    """
                )

            command.Parameters.AddWithValue(dto.DockerImage) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }

    interface IPrograms with
        member val DataSource = None with get, set

        member this.create(dto: ProgramsDto) =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbCreate (dataSource) (dto)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return

        member this.read() =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbRead (dataSource)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return
