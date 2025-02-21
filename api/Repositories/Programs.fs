module Api.Repository.Programs

open Npgsql

open Api.Repository.IPrograms
open Api.Types

type ProgramsRepository() =
    member private _.dbCreate (dataSource: NpgsqlDataSource) (dto: ProgramsDtoToDB) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    INSERT INTO programs
                      (id, program_name, created_at)
                    VALUES ($1, $2, $3);
                    """
                )

            command.Parameters.AddWithValue(dto.ProgramId) |> ignore
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
                      p.created_at
                    FROM programs p;
                    """
                )

            use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

            let mutable dbResponse = []

            while! (reader.ReadAsync() |> Async.AwaitTask) do
                let programId = reader.GetGuid(0)

                let programName = reader.GetString(1)

                let createdAt = reader.GetDateTime(2)

                dbResponse <-
                    { ProgramId = programId
                      ProgramName = programName
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
                    WHERE id = $2;
                    """
                )

            command.Parameters.AddWithValue(programFilePath) |> ignore
            command.Parameters.AddWithValue(programId) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }

    // XXX just sample, this is not currently used by the application
    member private _.dbDelete (dataSource: NpgsqlDataSource) (dto: ProgramsDtoInput) =
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

        member this.create(dto: ProgramsDtoToDB) =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbCreate (dataSource) (dto)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return

        member this.read() =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbRead (dataSource)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return

        member this.update (programId: System.Guid) (programFilePath: string) =
            match (this :> IPrograms).DataSource with
            | Some dataSource -> this.dbUpdate (dataSource) (programId) (programFilePath)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return
