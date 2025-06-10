// useful script to test the failure

let SUCCESS_STATUS_CODE = 0
let FAILURE_STATUS_CODE = 1

let main () : Async<int> =
    async {
        try
            let args = fsi.CommandLineArgs

            if args.Length < 2 then
                eprintfn "Please inform the time to wait!"
                exit FAILURE_STATUS_CODE
                return FAILURE_STATUS_CODE
            else
                let sleepTime = int args.[1]
                printfn "Sleeping for %i ms" sleepTime
                do! Async.Sleep(sleepTime)
                printfn "Sleep finished"

                exit SUCCESS_STATUS_CODE
                return SUCCESS_STATUS_CODE
        with exn ->
            eprintfn "An exception was thrown: %A" exn
            exit FAILURE_STATUS_CODE
            return FAILURE_STATUS_CODE
    }

main () |> Async.RunSynchronously
