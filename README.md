# Procedural Animation in Unity (Portfolio Project)

A Unity project exploring procedural animation techniques using Inverse Kinematics (IK) to create adaptive, real-time locomotion for digital creatures.

---

##  Portfolio & Project Showcase

This repository contains the source code for my Personal Portfolio project. **For a full, detailed breakdown of the project, including videos, technical explanations, and a reflection on the learning outcomes, please visit the official project Wiki.**

### ☆ [View the Full Project Portfolio on the Wiki](https://github.com/HangyBoi/Chunk-of-Procedural-Animation/wiki) ☆

---

## Project Description

This project features the implementation of two procedurally animated creatures:
1.  **An 8-Legged Spider:** Focuses on a robust, IK-driven tripod gait system that adapts to uneven terrain.
2.  **A Gecko:** A more advanced creature with a modular architecture that includes:
    *   Root motion for AI (target following) or manual player control.
    *   Dynamic body adjustment (height and rotation) to match ground topology.
    *   A diagonal gait stepping pattern.
    *   Independent head and eye tracking for a sense of awareness.
    *   Reactive tail animation based on body movement.

## Core Technologies
*   **Engine:** Unity 6.1 (6000.1.14f1)
*   **Language:** C#
*   **Key Packages:** Unity Animation Rigging (for LO2)

## How to Use
1.  Clone the repository.
2.  Open the project folder in a compatible version of the Unity Editor.
3.  The main demonstration scenes can be found in the `Assets/Scenes` folder.

## Acknowledgments
*   The advanced gecko implementation was guided by the excellent ["Bonehead" tutorial by weaverdev](https://weaverdev.io/projects/bonehead-procedural-animation/).
*   Main inspiration for 8-Leg Spider: ["Unity procedural animation tutorial (10 steps)" by Codeer](https://www.youtube.com/watch?v=e6Gjhr1IP6w) & ["Procedural Spider Showcase" by Sopiro](https://www.youtube.com/watch?v=pUp133rtDxM).
*   Absolute fundamentals laid down by Unity Learn in ["Prototyping Procedural Animation"](https://learn.unity.com/project/prototyping-a-procedural-animated-boss).
*   Initial research was informed by the articles on Inverse Kinematics by [Alan Zucconi](https://www.alanzucconi.com/).
