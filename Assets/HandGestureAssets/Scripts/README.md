# Setting Up Custom Gestures in your Own Project

## Initialisation of Project for MR Development

## Oculus

1. Change your build settings to android

## HoloLens

1. Change build settings to Universal Windows Platform
----

### Things to note:
- Gesture detection may vary depending on differences in hand size - try experimenting with different threshold values in the 
- Number of joints in Oculus is only 22, while in HoloLens it is 26. Therefore, gestures recorded on one device may not register when in another. Recommended to record different gestures for different devices.