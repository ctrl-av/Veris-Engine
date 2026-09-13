local width, height = 960, 640
return {
    update = function()
        local viewportWidth, viewportHeight = engine.viewport()
        engine.camera(width / 2, height / 2, math.min(viewportWidth / width, viewportHeight / height), 0)
        local _, pressed = engine.action("Restart")
        if pressed then engine.transition("Scenes/arena.verisscene") end
    end
}
