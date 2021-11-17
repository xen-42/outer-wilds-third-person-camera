# Outer Wilds Third Person Camera

![3rd person](https://user-images.githubusercontent.com/22628069/142057019-e2dcca28-6838-4b94-b45a-29843d44ab62.png)

Press V or the left analog stick to toggle between 1st and 3rd person.

Scroll in/out to zoom in/out.

Current issues:

- Artifacts appear ontop of you. (Replace it with my own gameobject?)
- Probes are invisible in 3rd person. (Replace with a new gameobject?)
- The player can see ghost matter in 3rd person. (I know how to fix it but it makes your head disappear).
- Statue doesn't re-enable 3rd person after uplink
- Stranger dreams don't do the thing when you do the one thing
- Everything breaks horribly if you try to switch profiles
- Ship target lock-on breaks sometimes

TODO:

- Add deconstructor?
- Remove crosshair
- Add helmet and ship console GUI to camera screen
- Add dream player model
- Should go first person when:
-   Interacting with grabby things
-   Interacting with grabby guys 
- Do the wake up particle effects when waking up in the stranger.


Wishlist:
- Make tools work in 3rd person (ProbeLauncher needs UI, SignalScope needs model and better zoom/aiming (over the shoulder?), Translator needs model and scan effect and text on screen).
- Free rotate camera 360 and with player camera (not just ship).
- Actually catch fire in fires
