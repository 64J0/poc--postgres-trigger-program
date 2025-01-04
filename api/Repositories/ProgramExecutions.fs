module Api.Repository.ProgramExecutions

open Npgsql

open Api.Types

let getAll (dataSource: NpgsqlDataSource) : Async<Result<ProgramExecutionsDtoOutput list, string>> =
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
        JOIN program_outputs po
        ON po.execution_id = pe.id
        """
            )

        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

        let mutable dbResponse = []

        while! (reader.ReadAsync() |> Async.AwaitTask) do
            let name = reader.GetString(0)
            let dockerImage = reader.GetString(1)
            let programInput = reader.GetString(2)
            let pullSuccess = reader.GetBoolean(3)
            let stdOutLog = reader.GetString(4)
            let stdErrLog = reader.GetString(5)

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

// let create (dataSource: NpgsqlDataSource) (dto: ProgramExecutionsDtoInput) : Async<Result<unit, string>> =
//     async {
//         let command =
//             dataSource.CreateCommand(
//                 """
//         INSERT INTO program_executions
//         SELECT p.id, $2, $3 FROM programs p WHERE p.name = $1
//         """
//             )

//         command.Parameters.AddWithValue(dto.Name) |> ignore
//         command.Parameters.AddWithValue(dto.ProgramInput) |> ignore
//         command.Parameters.AddWithValue(dto.CreatedAt) |> ignore

//         let! _ = command.ExecuteNonQueryAsync() |> Async.AwaitTask

//         return Ok()
//     }
