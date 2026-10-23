# Development log

## Session 1 — October 23, 2026

Chapter 6, The Cloud Observatory, asks both partners to ride a moving lift and step onto the roof. The lift stops level with that roof, but its deck ends about 30 pixels short of the platform. Walking off used to drop a partner into the gap. The roof's side then stopped them and zeroed their speed, so the miss became a fall toward the hazard under the observatory.

Horizontal collision now treats a lip within 14 pixels of a partner's feet as a step. Partners can walk off the lift onto the roof. Closed gates and taller walls still block. The editor gameplay check covers the walk-off and confirms a shut vault gate does not turn into a step.
