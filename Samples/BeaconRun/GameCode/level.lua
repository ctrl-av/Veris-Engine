local duration, elapsed, score = 35, 0, 0
local player, droneA, droneB, sound, marker
local signals, scores = {}, {}
return {
    start = function()
        player = engine.find("Player")
        droneA, droneB = engine.find("Drone A"), engine.find("Drone B")
        sound, marker = engine.find("Signal sound"), engine.find("Timer marker")
        engine.set_position(droneA, 570, 180)
        engine.set_position(droneB, 450, 500)
        for count = 0, 3 do scores[count] = engine.find("Score " .. count) end
        signals[engine.prefab("Prefabs/signal.verisprefab", 180, 160)] = true
        signals[engine.prefab("Prefabs/signal.verisprefab", 800, 180)] = true
        signals[engine.prefab("Prefabs/signal.verisprefab", 480, 560)] = true
    end,
    update = function(dt)
        elapsed = elapsed + dt
        local width, height = engine.viewport()
        engine.camera(480, 320, math.min(width / 960, height / 640), 0)
        engine.set_velocity(droneA, 264 * math.cos(elapsed * 1.2), 0)
        engine.set_velocity(droneB, 352 * math.cos(elapsed * 1.6), 0)
        engine.set_position(marker, 800 - 300 * math.min(elapsed / duration, 1), 62)
        for _, contact in ipairs(engine.contacts(player)) do
            if contact.phase == "begin" then
                if contact.other == droneA or contact.other == droneB then
                    engine.transition("Scenes/lose.verisscene")
                    return
                end
                if signals[contact.other] then
                    signals[contact.other] = nil
                    engine.destroy(contact.other)
                    engine.play_audio(sound)
                    score = score + 1
                    for count = 0, 3 do engine.tint(scores[count], count == score and 0xFFFFFFFF or 0xFFFFFF00) end
                end
            end
        end
        if score == 3 then engine.transition("Scenes/win.verisscene")
        elseif elapsed >= duration then engine.transition("Scenes/lose.verisscene") end
    end
}
