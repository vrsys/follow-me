# follow-me

This Unity3D projects contains the scripts and scenes that have been used for the user study reported in the paper "Follow Me: Confirmation-based Group Navigation in Collocated Virtual Reality".

The model files for the study have been removed, since they came from a licensed assett package from the Unity Assett Store that is not allowed to be shared publicly.
The assett package is called "Art Gallery Vol. 10" (https://assetstore.unity.com/packages/3d/environments/art-gallery-vol-10-260671).

Before starting the application, the project has to be linked to a Unity Cloud Project under Edit/Project Settings/Services.

To start the application, use the scene "Study-Lobby", that is located under Assetts/GroupNavigation-Study/Scenes.
The first user to join should now select the role "Guide". Following users should join as "Visitor".

The button mappings are as followed:

Guide
    Right Controller
        - Meta-Button: Keep pressed to recenter (Gaze direction = forward direction)
        - A-Button: UI Interaction & Stop Audio Guide
        - B-Button: no function
        - Thumbstick-Press: Toggle Right Hand Ray
        - Thumbstick-Movement: no function
        - Trigger-Button: no function
        - Grip-Button: trigger jump to right ray selected poi

    Left Controller
        - Menu-Button: no function
        - X-Button: Start Audio Guide
        - Y-Button: toggle wrist menu
        - Thumbstick-Press: Toggle Left Hand Ray
        - Thumbstick-Movement: no function
        - Trigger-Button: no function
        - Grip-Button: trigger jump to left ray selected poi

Visitor 
    Right Controller
        - Grip-button press: start direction estimation
        - Grip-button release: login direction estimation

For more information reach out to tony.jan.zoeppig@uni-weimar.de