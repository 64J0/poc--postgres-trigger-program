// useful script to test the failure

let wait (time: int) =
    printfn "Waiting for %i ms" time
    System.Threading.Thread.Sleep time
    printfn "Wait finished"

let args = fsi.CommandLineArgs

if args.Length < 2 then
    eprintfn "Please inform the time to wait!"
else
    wait (int args.[1])
