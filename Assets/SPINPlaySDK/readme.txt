SPIN Play SDK for Unity
Copyright (c) Pixvana, Inc. All rights reserved.

Notes
-----
When "Lock to Hmd" is enabled on the [CameraRig] component of the [Projector], the [CameraRig] component continuously
positions the [Projector] to offset the position of the [Hmd], effectively keeping the [LeftEyeCamera] and [RightEyeCamera]
at 0, 0, 0. So, don't add anything as a child of the [Projector], or it will move with the [Projector].

In Unity 5.6, the console may continuously report an "IsMatrixValid(matrix)" error. This is a known Unity bug (#899259)
that can be ignored. https://fogbugz.unity3d.com/default.asp?899259_2kldo1v6uil1s7mf

In Unity 5.6 (and above), the console may report "Multiple plugins with the same name 'ovrplugin'." According to the official
Unity Developer Guide from Oculus, it's a known issue and can be ignored. page 84: "Unity 5.6 and later: If you have updated
your OVRPlugin version from Utilities, you may see a spurious error message when the Editor first launches saying
“Multiple plugins with the same name 'ovrplugin'”. Please disregard."
http://static.oculus.com/documentation/pdfs/game-engines/latest/unity.pdf

There seems to be an issue with the use of the "Single Pass" Stereo Rendering Method in Unity 5.6.0f3 (and perhaps other
versions of 5.6) that worked correctly in Unity 5.5.0f3. In cases where the video doesn't playback correctly, try changing
the Stereo Rendering Method to "Multi Pass".

Added "DisableCameraDuringPlayback" component. Add to a camera to enable/disable it based on [Player] events. Disabling
any non-[CameraRig] cameras during playback will reduce the rendering workload and improve overall performance.

Added "ProjectionScaleControl" component. Add to a [Projector], and use the left and right bracket keys to interactive
modify the monoProjectionScale and stereoProjectionScale values of the [Projector]. Intended for debugging scale values.

When building for the Windows Store (Windows MR):

    * Set "SDK" to "Universal 10" in File/Build Settings...
    * Set "Target device" to "PC" in File/Build Settings... (other devices are not supported)
    * Set "UWP SDK" to either "Latest installed" or at least "10.0.15063.0" (the versions are listed in the dropdown)
      in File/Build Settings...
    * Review the Publishing Settings/Capabilities under the Edit/Project Settings/Player menu. For example, select
      InternetClient when playing media over the network.

Ambisonic audio support on Android uses an underlying Google ExoPlayer extension that depends on the Google VR SDK. If native
Daydream and/or Cardboard VR support is enabled within Unity, there is a pre-build script under Editor/AmbisonicBuildCheck.cs
that runs to automatically include/exclude appropriate SPIN Play SDK Google VR aar dependencies.