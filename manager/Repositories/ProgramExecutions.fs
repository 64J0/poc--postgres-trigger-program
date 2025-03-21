namespace Manager.Repositories

module ProgramExecutions =

    open Npgsql

    open Manager.Types

    let getById (dataSource: NpgsqlDataSource) (programExecutionId: int) =
        async {
            try
                use command =
                    dataSource.CreateCommand(
                        """
                        SELECT
                            pe.id,
                            p.program_name,
                            pe.program_input,
                            pe.created_at,
                            p.program_file_path
                        FROM program_executions pe
                        JOIN programs p
                        ON pe.program_id = p.id
                        WHERE pe.id = $1;
                        """
                    )

                command.Parameters.AddWithValue(programExecutionId) |> ignore

                use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

                let dbResponse: ProgramExecutionsDto list =
                    [ while reader.Read() do
                          let id = reader.GetInt32(0)
                          let programName = reader.GetString(1)
                          let programInput = reader.GetString(2)
                          let createdAt = reader.GetDateTime(3)
                          let programFilePath = Shared.Database.Main.tryGetValue reader.GetString 4

                          { Id = id
                            ProgramName = programName
                            ProgramInput = programInput
                            CreatedAt = createdAt
                            ProgramFilePath = programFilePath } ]

                return
                    match List.tryHead dbResponse with
                    | Some hd -> Ok hd
                    | None ->
                        Error(Database $"No program execution entity found at the database for id {programExecutionId}")
            with exn ->
                return Error(Database $"Database handling error: {exn.Message}")
        }
