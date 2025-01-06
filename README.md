# follow-me

This Unity3D projects contains the scripts and scenes that have been used for the user study reported in the paper "Follow Me: Confirmation-based Group Navigation in Collocated Virtual Reality".

The model files for the study have been removed, since they came from a licensed asset package from the Unity Asset Store that is not allowed to be shared publicly.
The asset package is called "Art Gallery Vol. 10" (https://assetstore.unity.com/packages/3d/environments/art-gallery-vol-10-260671).

Before starting the application, the project has to be linked to a Unity Cloud Project under _Edit/Project Settings/Services_.

To start the application, use the scene "Study-Lobby", that is located under _Assets/GroupNavigation-Study/Scenes_.
The first user to join should now select the role "Guide". Following users should join as "Visitor".

The button mappings are as followed:

1. Guide
    * Right Controller
        - Meta Button: Keep pressed to recenter (Gaze direction = forward direction)
        - A-Button: UI interaction & stop audio guide
        - Thumbstick Press: Toggle right hand ray
        - Grip Button: Trigger jump to right ray selected ROI
    * Left Controller
        - X Button: Start audio guide
        - Y Button: Toggle wrist menu
        - Thumbstick Press: Toggle left hand ray
        - Grip Button: Trigger jump to left ray selected ROI
2. Visitor
    * Right Controller
        - Grip Button Press: Start direction estimation
        - Grip Button Release: Confirm directon estimation
     
Under _Assets/GroupNavigation-Study/Questionnaire_ you can find a PDF including the questionnaire used for the study.

For more information reach out to tony.jan.zoeppig@uni-weimar.de
