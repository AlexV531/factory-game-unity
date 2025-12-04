Dissonance Offline Voice
========================

This package provides a network integration for Dissonance Voice Chat (https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078?aid=1100lJDF)
which always pretends to be online, does not require any actual network connection and sends your own voice back to yourself.

This can be used anywhere that you want to run the Dissonance audio pipeline without having to connect to a network. For example
in a settings menu, where you may want to hear yourself as you tweak mic settings.

The included demo scene (Assets/Dissonance/Integrations/Offline/Demo/Offline Demo Scene) demonstrates the basics of a voice
chat session running offline. You should hear yourself speaking.



Hearing Yourself: Loopback Audio
================================

All voice packets sent to this network system get sent back from a fake user named "Loopback" in a fake room named "Loopback".

To hear yourself:
1. Create a Dissonance "Voice Broadcast Trigger", sending to a room named "Loopback".
2. Create a Dissonance "Voice ReceiptTrigger", listening to a room named "Loopback".



Documentation
=============

Dissonance includes detailed documentation on how to properly install and use Dissonance, you can find this
documentation online at:

	https://placeholder-software.co.uk/Dissonance/docs

There is a compressed copy of this documentation included in the package for offline access. You can find this at:

	Assets/Dissonance/Offline Documentation~.zip

Extract the archive and open `index.html` to get started



Project Setup
=============

Because Dissonance is a realtime communication system you must set your project to run even when it does not have
focus. To do this go to:

	Edit -> Project Settings -> Player
	
Check the `Run In Background` box in the inspector.



Further Support
===============

If you encounter a bug or want to make a feature request please open an issue on the issue tracker:

	https://github.com/Placeholder-Software/Dissonance/issues

If you have any other questions ask on the discussion forum:

	https://www.reddit.com/r/dissonance_voip/
	
Or send us an email:

	mailto://admin@placeholder-software.co.uk