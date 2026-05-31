# Spear Arcade

A 3D tactical survival game built in the Godot Engine. Players defend against incoming enemy ambushes, utilize real-time structural cover, and hurl spears at tracking targets within an adaptive arena loop.

---

## Core Systems and Mechanics

### Tactical Cover Environment
The environment utilizes physics-mapped cabins and foliage to break enemy line-of-sight and establish defensive choke points. These primitive geometries serve as fully functional barriers against projectile or melee advancement.

### Navigation and Tracking AI
Enemies use real-time pathfinding to track player coordinates continuously. The logic allows actors to navigate around solid obstacles, parse architectural entryways, and dynamically enter interior spaces to flush out the player.

### Concealed Data Framework
To heighten gameplay tension, numerical feedback is absent from the player interface. Health bars, explicit damage readouts, and operational values remain hidden, requiring players to gauge threats based entirely on environmental interaction, visual sizing, and sound cues.

### Dynamic Enemy Archetypes
While numerical stats are obscured, threat verification is handled via 3D text billboards floating above each enemy instance. These track explicit visual variants adjusted on spawn:
* **Small Brown Robber:** Optimized for velocity, lower total health, and a smaller visual/physical profile.
* **Medium Silver Soldier:** Set to baseline architectural dimensions, featuring a distinctive reflective metallic material pass.
* **Big Red Demon:** Massively scaled configuration utilizing increased uniform height and wide physical boundaries combined with a deep crimson color assignment.

---

## Technical Infrastructure

This repository demonstrates the integration of multiple standalone systems within Godot 4.x (C#):
* **Dynamic Physics Profile Scaling:** Programmatic mesh-to-collision syncing using custom Capsule shapes. Physical properties update at runtime to match localized database values.
* **Projectile Forward-Vector Offsets:** Positional translation offsets implemented on projectile instantiation. This prevents raw projectiles from prematurely clipping through bounding volumes when standing near walls or under sloped roofs.
* **Decoupled API Token Routing:** System configuration reads localized files directly from the client sandboxed data directory (`user://api_key.txt`), removing the need to embed production authentication tokens inside the version-controlled repository or compiled executable.

---

## Operational Guide

### API Authentication (Bring Your Own Key)
The dynamic level generator relies on cloud-based structural analysis to scale enemy waves. 
1. Launch the application to access the Main Menu.
2. Insert a personal Gemini API Key into the secure input text block.
3. Select a game initialization button to securely cache the string to the local disk.

*Security Protocol: Authentication parameters are maintained entirely inside the native OS application sandbox and are transmitted solely via direct outbound requests to official API endpoints.*

### Game Execution
1. Navigate to the Releases container on the right panel of this repository.
2. Download the compressed executable package (`SpearArcade_Windows.zip`).
3. Extract the zipped payload and run `SpearArcade.exe` to initialize the runtime window.

### Control Layout
* **Movement:** WASD or Arrow Keys
* **Orientation / Aiming:** Mouse Look
* **Action (Hurl Projectile):** Left Mouse Button
