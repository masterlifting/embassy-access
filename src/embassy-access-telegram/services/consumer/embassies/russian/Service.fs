module EA.Telegram.Services.Consumer.Embassies.Russian.Service

open EA.Telegram.Services.Consumer.Embassies.Russian

module Kdmid =
    module Query =
        let getData = Kdmid.Query.getData
        let checkAppointments = Kdmid.Query.checkAppointments

    module Command =
        let subscribe = Kdmid.Command.subscribe
        let checkAppointments = Kdmid.Command.checkAppointments
        let sendAppointments = Kdmid.Command.sendAppointments
        let confirmAppointment = Kdmid.Command.confirmAppointment
        let createInstruction = Kdmid.Command.Instruction.create

module Midpass =
    module Query =
        let checkStatus = Midpass.Query.checkStatus

module Query =
    let get = Kdmid.Query.getData
    
module Command =
    let run = Kdmid.Command.createInstruction