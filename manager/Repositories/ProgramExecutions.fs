namespace Manager.Repositories

module ProgramExecutions =

    open Npgsql

    open Manager.Types

    let getById (dataSource: NpgsqlDataSource) (programExecutionId: int) =
        async {
            use command =
                dataSource.CreateCommand(
                    """
                    SELECT
                      pe.id,
                      pe.program_name,
                      pe.program_input,
                      pe.created_at
                    FROM program_executions pe
                    WHERE pe.id = $1;
                    """
                )

            command.Parameters.AddWithValue(programExecutionId) |> ignore

            use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

            let mutable dbResponse = []

            while! (reader.ReadAsync() |> Async.AwaitTask) do
                let id = reader.GetInt32(0)
                let programName = reader.GetString(1)
                let programInput = reader.GetString(2)
                let createdAt = reader.GetDateTime(3)

                dbResponse <-
                    { Id = id
                      ProgramName = programName
                      ProgramInput = programInput
                      CreatedAt = createdAt }
                    :: dbResponse

            return
                match List.tryHead dbResponse with
                | Some hd -> Ok hd
                | None ->
                    Error(Database $"No program execution entity found at the database for id {programExecutionId}")
        }
