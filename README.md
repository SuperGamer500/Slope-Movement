# Slope-Movement

Thank you for downloading my assets. This series of assets will help make slope movment a lot easier.

Important Information (Read this part of this file)
1. Use the provided physics material 2D on every object that you want to use slopes. If you do not, there is a chance that objects will get caught on slopes. I would recommend setting the physics material as the default in Project Settings > Phyics2D > Default Material
2. Turn off Query Hit Triggers (Project Settings > Phyics2D) and Queries in Colliders (Project Settings > Phyics2D) to prevent slope raycasting to hit the current object and any trigger objects present
3. Make sure to set the player character's layer to one called "Player". If you don't do this, the script will not detect input from a keyboard or controller.
4. Create 2 objects for the slope and ground object or the script will not work. The slope check object determines when the obj will detect slopes, the length determines how long each ray will go downwards and its overall scale will determine if the object will use the slope information. If the slope check is not overlapping with specified ground layers, slopes will not be calculated. There is an option labeled "Dynamic Slope Check" that will handle the scaling of the slope check obj if needed.
5. I would recommend using tilemaps because it makes collision detection more smooth.
6. Avoid making object move to fast with the script because it could cause the slope check to break.

Copyright Information
Dont worry about copyright for these assets. If you want to credit me you can copy/paste the text below.

"Slope-Movement" by SuperGamer500 https://www.youtube.com/channel/UCdIrqcHNYux_0OSNiFJHNZQ


    
    

