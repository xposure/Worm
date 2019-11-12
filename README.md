# Worm
https://www.reddit.com/r/gamedev/comments/1eg21z/how_do_you_implement_acceleration/

I personally moved from a force/mass/acceleration model to a simpler, less realistic but more controllable/reliable dampening method for most of my entities.

I did this because my force/acceleration model wasn't working for humanoids (and nor should it, it's an inappropriate model, legged creatures aren't billiard balls).

Basically:

New Velocity = old_velocity * (1 - delta_time * transition_speed) + desired_velocity * (delta_time * transition_speed)

(we're just lerping here)

transition_speed is one of a few constants (based on terrain type) largely simulated friction, water/ground has a transition speed of 4.0, and to have a smidge of air control, the air transition speed is 0.7.

desired_velocity is a unit length "running force" (whatever the entity is trying to do) multiplied by the characters max speed for what they're standing on, e.g. higher for land than water.

Implementing this immediately made my FPS experience immediately feel much tighter, and all issues I had applying f = ma to humanoids disappeared.

Projectiles are still doing f = ma (with drag etc.), but I only apply it to new entities if the simple model fails me.