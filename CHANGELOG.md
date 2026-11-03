# Development log

## Session 7 — November 3, 2026

Standing in a home arch used to do nothing when the chapter was not ready. If one partner arrives early, the game now explains the missing step: unsecured crossings, unfinished keepsakes, or a partner who has not reached home yet. The hint uses a short cooldown so it does not spam while they wait.

## Session 6 — November 2, 2026

Final review pass. Movement and jump values now live as named constants shared with the timed-bridge fade threshold. Chapter 7's tip mentions the one-time fade warning. The README documents controls, Unity play steps, and the Play Mode validation menu. Earlier session changes were re-checked against the shared Simulate path used by live play and the editor assertions.

## Session 5 — November 2, 2026

Pause now shows time spent together and retry count for the current chapter. The complete screen shows retries too, and Escape from a finished chapter opens the chapter map instead of doing nothing. Map Back returns to play when a chapter is already underway, otherwise to the title. The title control hints match the hold-to-jump controls, and returning to the title clears leftover toast text.

## Session 4 — November 2, 2026

Partners now face the direction they are moving, stretch slightly while rising through a jump, and squash for a short beat on landing. A soft ground flash marks the impact. The original pixel sprites are unchanged; only the draw size and horizontal flip change with motion.

## Session 3 — November 2, 2026

Timed bridges already show a countdown, but partners standing on the deck still had no clear cue that the charge was about to fail. When a timed crossing drops to 1.2 seconds or less, is no longer held, and a partner is still on the deck, the game now shows one "Bridge fading — keep moving!" toast for that charge. Holding the plate again clears the warning so a later fade can warn once more.

## Session 2 — October 28, 2026

Jump height no longer ignores the key after the press. Holding W or Up keeps the full leap. Releasing it while a partner is still rising adds gravity, so a tap is a shorter hop. Letting go of A, D, or the arrows on the ground now stops in a shorter step instead of sliding the same distance it takes to reach full speed.

A jump pressed while falling into a hazard used to stay buffered. The partner landed on the respawn floor and immediately leaped off it. Respawn now clears that buffered jump, the coyote window, and leftover speed.

## Session 1 — October 23, 2026

Chapter 6, The Cloud Observatory, asks both partners to ride a moving lift and step onto the roof. The lift stops level with that roof, but its deck ends about 30 pixels short of the platform. Walking off used to drop a partner into the gap. The roof's side then stopped them and zeroed their speed, so the miss became a fall toward the hazard under the observatory.

Horizontal collision now treats a lip within 14 pixels of a partner's feet as a step. Partners can walk off the lift onto the roof. Closed gates and taller walls still block. The editor gameplay check covers the walk-off and confirms a shut vault gate does not turn into a step.
