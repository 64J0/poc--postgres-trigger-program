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
                      (id, program_name, created_at)
                    VALUES ($1, $2, $3);
                    """
                )

            let newId = System.Guid.NewGuid()

            command.Parameters.AddWithValue(newId) |> ignore
            command.Parameters.AddWithValue(dto.ProgramName) |> ignore
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
                      p.id,
                      p.program_name,
                      p.program_file_path,
                      p.created_at
                    FROM programs p;
                    """
                )

            use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

            let mutable dbResponse = []

            while! (reader.ReadAsync() |> Async.AwaitTask) do
                let programId = reader.GetString(0) |> System.Guid
            
                let programName = reader.GetString(1)

                let programFilePath =
                    match reader.IsDBNull(2) with
                    | true -> None
                    | false -> Some(reader.GetString(2))

                let createdAt = reader.GetDateTime(3)

                dbResponse <-
                    { Id = Some programId
                      ProgramName = programName
                      ProgramFilePath = programFilePath
                      CreatedAt = createdAt }
                    :: dbResponse

            return Ok dbResponse
        }

    member private _.dbUpdate (dataSource: NpgsqlDataSource) (programId: System.Guid) (programFilePath: string) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    UPDATE programs
                    SET program_file_path = $1
                    WHERE program_id = $2;
                    """
                )

            command.Parameters.AddWithValue(programFilePath) |> ignore
            command.Parameters.AddWithValue(programId) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }

    // XXX just sample, this is not currently used by the application
    member private _.dbDelete (dataSource: NpgsqlDataSource) (dto: ProgramsDto) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    DELETE FROM programs
                    WHERE program_name = $1;
                    """
                )

            command.Parameters.AddWithValue(dto.ProgramName) |> ignore

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

        member this.update(programId: System.Guid) (programFilePath: string) =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbUpdate (dataSource) (programId) (programFilePath)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return
