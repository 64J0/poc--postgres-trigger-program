module Api.Repository.Programs

open Npgsql

open Api.Types

let getAll (dataSource: NpgsqlDataSource) : Async<Result<ProgramsDto list, string>> =
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

let create (dataSource: NpgsqlDataSource) (dto: ProgramsDto) : Async<Result<unit, string>> =
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
