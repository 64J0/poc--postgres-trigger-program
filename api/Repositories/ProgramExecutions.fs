module Api.Repository.ProgramExecutions

open Npgsql

open Api.Repository.IProgramExecutions
open Api.Types

type ProgramExecutionsRepository() =
    member private this.dbCreate (dataSource: NpgsqlDataSource) (dto: ProgramExecutionsDtoInput) =
        async {
            let command =
                dataSource.CreateCommand(
                    """
                    INSERT INTO program_executions
                      (program_id, program_input, created_at)
                    VALUES ($1, $2, $3);
                    """
                )

            command.Parameters.AddWithValue(dto.ProgramId) |> ignore
            command.Parameters.AddWithValue(dto.ProgramInput) |> ignore
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
                      p.program_name, 
                      p.program_file_path, 
                      pe.program_input, 
                      po.pull_success, 
                      po.stdout_log, 
                      po.stderr_log 
                    FROM programs p
                    JOIN program_executions pe
                    ON pe.program_id = p.id
                    LEFT JOIN program_outputs po
                    ON po.execution_id = pe.id;
                    """
                )

            use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

            let mutable dbResponse = []

            while! (reader.ReadAsync() |> Async.AwaitTask) do
                let programName = reader.GetString(0)

                let programFilePath =
                    match reader.IsDBNull(1) with
                    | true -> None
                    | false -> Some(reader.GetString(1))

                let programInput = reader.GetString(2)
                let pullSuccess = Shared.Database.Main.tryGetValue<bool> (reader.GetBoolean) 3
                let stdOutLog = Shared.Database.Main.tryGetValue<string> (reader.GetString) 4
                let stdErrLog = Shared.Database.Main.tryGetValue<string> (reader.GetString) 5

                dbResponse <-
                    { ProgramName = programName
                      ProgramFilePath = programFilePath
                      ProgramInput = programInput
                      PullSuccess = pullSuccess
                      StdOutLog = stdOutLog
                      StdErrLog = stdErrLog }
                    :: dbResponse

            return Ok dbResponse
        }

    interface IProgramExecutions with
        member val DataSource = None with get, set

        member this.create(dto: ProgramExecutionsDtoInput) =
            match (this :> IProgramExecutions).DataSource with
            | Some dataSource -> this.dbCreate (dataSource) (dto)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return

        member this.read() =
            match (this :> IProgramExecutions).DataSource with
            | Some dataSource -> this.dbRead (dataSource)
            | None -> Error(ApplicationError.Database "DataSource object was not set") |> async.Return
