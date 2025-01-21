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
                        SELECT p.id, $2, $3 
                        FROM programs p 
                        WHERE p.name = $1;
                        """
                )

            command.Parameters.AddWithValue(dto.Name) |> ignore
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
                          p.name, 
                          p.docker_image, 
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
                let name = reader.GetString(0)
                let dockerImage = reader.GetString(1)
                let programInput = reader.GetString(2)
                let pullSuccess = Api.Database.tryGetValue<bool> (reader.GetBoolean) 3
                let stdOutLog = Api.Database.tryGetValue<string> (reader.GetString) 4
                let stdErrLog = Api.Database.tryGetValue<string> (reader.GetString) 5

                dbResponse <-
                    { Name = name
                      DockerImage = dockerImage
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
            | None -> Error "DataSource object was not set" |> async.Return

        member this.read() =
            match (this :> IProgramExecutions).DataSource with
            | Some dataSource -> this.dbRead (dataSource)
            | None -> Error "DataSource object was not set" |> async.Return
