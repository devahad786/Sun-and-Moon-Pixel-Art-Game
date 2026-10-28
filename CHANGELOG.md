# Development log

## Session 2 — October 28, 2026

Jump height no longer ignores the key after the press. Holding W or Up keeps the full leap. Releasing it while a partner is still rising adds gravity, so a tap is a shorter hop. Letting go of A, D, or the arrows on the ground now stops in a shorter step instead of sliding the same distance it takes to reach full speed.

A jump pressed while falling into a hazard used to stay buffered. The partner landed on the respawn floor and immediately leaped off it. Respawn now clears that buffered jump, the coyote window, and leftover speed.

## Session 1 — October 23, 2026

Chapter 6, The Cloud Observatory, asks both partners to ride a moving lift and step onto the roof. The lift stops level with that roof, but its deck ends about 30 pixels short of the platform. Walking off used to drop a partner into the gap. The roof's side then stopped them and zeroed their speed, so the miss became a fall toward the hazard under the observatory.

Horizontal collision now treats a lip within 14 pixels of a partner's feet as a step. Partners can walk off the lift onto the roof. Closed gates and taller walls still block. The editor gameplay check covers the walk-off and confirms a shut vault gate does not turn into a step.
