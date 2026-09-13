return {
    update = function()
        local right = engine.action("MoveRight")
        local left = engine.action("MoveLeft")
        local down = engine.action("MoveDown")
        local up = engine.action("MoveUp")
        local x = (right and 1 or 0) - (left and 1 or 0)
        local y = (down and 1 or 0) - (up and 1 or 0)
        local length = math.sqrt(x * x + y * y)
        if length > 0 then x, y = x / length, y / length end
        engine.set_velocity(self, x * 220, y * 220)
        engine.animation(self, length > 0)
    end
}
