# Game Mechanics Documentation

## 1. Game Overview
This game is a first-person maze challenge.  
The player must navigate a maze, avoid hazards and guards, push a physics key object (a box), and make that key collide with the door to win.

The level contains multiple navigable paths:
- A path that leads through challenge areas (trap/guards) toward the key.
- A path that leads to the door for game completion.

## 2. Core Objective
1. Find and reach the key object (box).
2. Push the key through the maze using player movement and physics interaction.
3. Make the key collide with the door.
4. Door opens and game ends with a replay prompt.

## 3. Player Mechanics
The player uses first-person movement:
- `WASD`: move
- `Shift`: sprint
- `Mouse`: look around
  - Vertical mouse affects camera pitch only.
  - Horizontal mouse affects camera yaw and player facing direction.
- `Space`: jump

Additional movement behavior:
- Grounded motion is tuned to reduce drifting.
- The player can push rigidbody objects (used for moving the key).

## 4. Key and Door Mechanics
### Key Object
- Implemented as a physics-enabled box.
- Uses rigidbody + collider (gravity, forces, collisions).
- Can be pushed by the player.

### Door Object
- Door is controlled by `SlidingDoor.cs`.
- Door opens only when an object tagged `Box` collides with it.
- Door opening is animated by sliding left/right parts.
- When fully open, the game triggers the end-of-round prompt.

## 5. Trap Mechanics (Coroutine-Based)
Spike traps are controlled by `PeriodicTrap.cs`:
- Trap behavior runs periodically using coroutines.
- Trap movement/phase cycles over time (active/inactive pattern).
- Collision/hit detection is used to kill the player when caught.
- On death, the replay UI can be shown.

This satisfies the periodic challenge requirement and coroutine usage requirement.

## 6. Guard Mechanics (Coroutine-Based)
Guards are controlled by `GuardPatrol.cs`:
- Guards patrol between waypoints continuously using a coroutine.
- Patrol supports ping-pong movement (A -> B -> A).
- Detection uses:
  - distance check,
  - front-angle (vision cone) check,
  - optional line-of-sight check.
- If player is detected in danger zone, player fails and replay UI is shown.

## 7. Win/Lose Flow and UI
A shared UI prompt (`DeathRoundPromptUI.cs`) is used for both:
- losing states (trap/guard death),
- winning state (door opened by key collision).

Prompt behavior:
- Shows a simple panel with two buttons:
  - `YES` -> restart current scene (another round),
  - `NO` -> quit game (or stop play mode in Unity Editor).

## 8. Audio
Background music is managed by `MusicToggle.cs`:
- Music plays in the scene with an `AudioSource`.
- Press `M` to toggle music on/off during gameplay.

## 9. Additional Usability
- A minimap camera is added to the Game View to help navigation in the maze.

## 10. Assets and Environment
Environment, character, and gameplay objects are built using Unity assets/prefabs (including Unity starter/store assets), combined with custom scripts for game logic.

## 11. Script Summary
- `PlayerMovement.cs`: first-person movement, camera look, push behavior.
- `SlidingDoor.cs`: key-door collision opening + win trigger.
- `PeriodicTrap.cs`: periodic trap cycle and player kill handling.
- `GuardPatrol.cs`: waypoint patrol + player detection fail condition.
- `DeathRoundPromptUI.cs`: replay/quit round prompt UI.
- `MusicToggle.cs`: background music toggle with key `M`.

## 12. Map Screenshot
<img width="874" height="878" alt="Screenshot 2026-03-17 at 23 46 42" src="https://github.com/user-attachments/assets/f05c8278-7d0e-47d6-a636-f041a5e59ea4" />


