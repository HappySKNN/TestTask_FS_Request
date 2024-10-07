module ListenerActor

open Akka.FSharp


let Listener (mailbox: Actor<string>) =
    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
        | msg -> 
            printf "%s" msg

        return! loop()
    }

    loop()
