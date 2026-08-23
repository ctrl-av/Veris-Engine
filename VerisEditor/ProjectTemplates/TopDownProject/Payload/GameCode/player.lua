local reached = false
return {
    update = function(dt)
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
        local px, py = engine.position(self)
        engine.camera(px, py, 1, 0)
        if not reached then
            local goal = engine.find("Goal")
            for _, contact in ipairs(engine.contacts(self)) do
                if contact.other == goal and contact.phase == "begin" then
                    reached = true
                    engine.tint(goal, 0x22C55EFF)
                    print("Goal reached")
                end
            end
        end
    end
}
