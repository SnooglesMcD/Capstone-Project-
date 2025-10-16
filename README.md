[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/6nx_gS-F)
# GDIM33 Demo Project
## Milestone 1 Devlog

Script Graph Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/Assets/PlayerInputScriptGraph.asset

Inputactions Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/Assets/verticalsliceplayerinput.inputactions

My understanding of the scope of my game has really humbled me, to be honest. I had a lot of lofty ideas of what I would be able to implement because visual scripting seemed so much simpler and more intuitive than writing lines of code, but I didn't anticipate how long it would take to set up graphs to do even simple things. It has made me want to change directions in terms of what the goals and premise of my game are, as I no longer think that it's feasible to accomplish what I had wanted without doing tons of extra research and study on the side.
## Milestone 2 Devlog

State Graph Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/Assets/Mr%20Wolf%20Movement.asset

NavMesh Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/ProjectSettings/NavMeshAreas.asset

Animation Controller Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/Assets/Player%20Animator.controller

In my game, I implemented a system for making Mr. Wolf walk between different objects on the farm. It went smoothly at first as I began implementing all the concepts I practiced in class, but I quickly ran into problems with the distance that I was comparing the objects between. I tried many different values, but none of them seemed to work the way I intended them to. I figured out that there was a discrepancy between the scaling of the collider boxes, as well as where the centers of each of the objects was located in the object, as well as using the prefab in the "get object" node on the MrWolfMovement graph that's attached to Mr.Wolf, versus using the actual instance of it in the scene. In the end, because of how the prefabs I chose are built, I couldn't do very much about the scaling and distance problems, so I had to pivot and change how I wanted Mr. Wolf to move, instead moving just between the watering can and the rake before stopping.

## Milestone 3 Devlog

Shader Graph Asset: https://github.com/UCI-GDIM33/vertical-slice-elanic3/blob/main/vertical-slice-elanic3/Assets/WaterTextureGraph.shadergraph

Gameplay Progress: I finished the interaction button so that pressing "e" or clicking the mouse button deactivates the old text, activates the new one, and instantiates a carrot farm all at once, completing the main task of the game.

This shader graph utilizes UV scrolling to give the water the appearance that it is flowing next to the farm. I wanted to do this to add to the cutesy almost Ghibli-like atmosphere of a farm with animals on it, and to make the water look less flat since the prefab I got from the asset store had no shading and flat color. It uses a SampleTexture2D node connected to an additive tint to produce this nice teal moving water. The UV scrolling works by changing the location of the mapping over time to change where the texture falls on the material. This fulfills my hopes to give the game cutesy vibes, but I've admittedly strayed from the idea of "cutesy post-apocalypse." I think it's working out best for my game and the assignment that I instead just focus on the cutesy vibe, however, so I'm quite pleased with the aesthetics. If I could change anything, I might go look for a different water texture that looks more cartoonish.

## Final Devlog

The player can walk around the farm, look for the shovel, and interact with it using [E] to complete the quest. 

The visual effect works by activating the outline renderer on start, which utilizes the stencil renderer components to create a bright yellow outline around the player character. This is helpful to the gameplay and aesthetic goals because it makes the character look cartoony and colorful, and also differentiates the player character from the NPC, which has a very similar model to it. This aims to decrease visual confusion when players look at the game.

## Open-source assets


https://assetstore.unity.com/packages/3d/characters/humanoids/character-pack-free-animal-people-sample-204568

https://assetstore.unity.com/packages/3d/environments/lowpoly-nature-village-pack-165318

https://assetstore.unity.com/packages/3d/props/pandazole-farm-ranch-low-poly-pack-206756

https://assetstore.unity.com/packages/3d/vegetation/plants/cartoon-farm-crops-79777

https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153