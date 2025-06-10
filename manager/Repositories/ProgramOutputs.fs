namespace Manager.Repositories

module ProgramOutputs =

    open Npgsql

    open Manager.Types

    let private transformOption (value: Option<'T>) =
        match value with
        | Some v -> v :> obj
        | None -> System.DBNull.Value :> obj

    let create (dataSource: NpgsqlDataSource) (dto: ProgramOutputDto) =
        async {
            let command =
                dataSource.CreateCommand(
                    """
                    INSERT INTO program_outputs
                      (execution_id,
                       execution_success,
                       status_code,
                       stdout_log,
                       stderr_log,
                       created_at)
                    VALUES ($1, $2, $3, $4, $5, $6);
                    """
                )

            command.Parameters.AddWithValue(dto.ExecutionId) |> ignore
            command.Parameters.AddWithValue(dto.ExecutionSuccess) |> ignore
            command.Parameters.AddWithValue(dto.StatusCode) |> ignore
            command.Parameters.AddWithValue(transformOption dto.StdOutLog) |> ignore
            command.Parameters.AddWithValue(transformOption dto.StdErrLog) |> ignore
            command.Parameters.AddWithValue(dto.CreatedAt) |> ignore

            let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

            return Ok()
        }
